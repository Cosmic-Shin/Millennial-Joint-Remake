using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
    [Header("Player")]
    public int playerNumber = 1; // 1 or 2

    [Header("Movement")]
    public bool doMove = true;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;  
    public float rotationSmooth = 14f;

    [Header("Sprint Stamina")]
    public float sprintDuration = 3f;
    public float sprintCooldown = 2f;

    [Header("Sprint Effects")]
    public ParticleSystem sprintDust;

    private float sprintTimer;
    private float sprintCooldownTimer;
    private bool sprintOnCooldown;

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

    [Header("Emote Select")]
    public KeyCode p1SelectKey = KeyCode.Z;
    public KeyCode p2SelectKey = KeyCode.Backspace;
    public string[] emoteSelectionLabels = new string[] { "Dance", "Shuffle", "Gangnam", "Breakdance" };
    public TextMesh emoteSelectionText;

    [Header("Emote Lock")]
    [SerializeField] private float emoteLockDuration = 2.5f;

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
    public ChiliPotCounter currentChiliPot;

    private bool isSelectingEmote;
    private bool isEmoting;
    private int selectedEmoteIndex;
    public GameObject emoteSelectionObject;
    public IngredientBox currentIngredientBox;
    public DrinkMachine currentDrinkMachine;

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

        sprintTimer = sprintDuration;
        sprintCooldownTimer = 0f;
        sprintOnCooldown = false;

        if (sprintDust != null)
        sprintDust.Stop();

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
        doMove = true;
        isSelectingEmote = false;
        isEmoting = false;
        if (currentIngredientBox != null)
        {
            Debug.LogWarning("PlayerControl: clearing stale currentIngredientBox reference at Start.", this);
            currentIngredientBox = null;
        }

        if (hitMask.value == 0)
            hitMask = ~0;

        CreateEmoteSelectionText();
        UpdateHeldItemHUD();
        RefreshHeldItemVisual();
        UpdateAnimator();
    }

    private void Update()
    {
        if (rb == null)
            return;

        EnsureMovementState();

        front = Input.GetKey(MoveUp);
        left = Input.GetKey(MoveLeft);
        back = Input.GetKey(MoveDown);
        right = Input.GetKey(MoveRight);

        if (!isSelectingEmote && !isEmoting && currentIngredientBox == null && currentDrinkMachine == null && currentChiliPot == null)
        {
            direction = new Vector3(
                (right ? 1 : 0) - (left ? 1 : 0),
                0f,
                (front ? 1 : 0) - (back ? 1 : 0)
            );
        }
        else
        {
            direction = Vector3.zero;
        }

        HandleEmoteSelectionInput();
        HandleEmotes();

        if (currentIngredientBox != null)
        {
            currentIngredientBox.HandleIngredientSelectionInput();
        }

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
                IngredientBox ingredientBox = interactable as IngredientBox;

                if (station != null && ingredientBox == null)
                {
                    if (lastOpenStation != null && lastOpenStation != station)
                        lastOpenStation.OpenPanel(false);

                    station.OpenPanel(true);
                    lastOpenStation = station;
                }
                else if (ingredientBox != null)
                {
                    currentIngredientBox = ingredientBox;
                }

                interactable.Interact(this);
                UpdateHeldItemHUD();
                RefreshHeldItemVisual();

                Debug.Log("AFTER INTERACT — " + GetHeldItemDebug()
                          + " | player instanceID: " + GetInstanceID());

                if (station != null && ingredientBox == null)
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

        if (currentDrinkMachine != null)
        {
            currentDrinkMachine.HandleDrinkSelectionInput();
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
        FaceEmoteTextToCamera();
    }

    private void FixedUpdate()
{
    if (!doMove || rb == null)
        return;

    if (isSelectingEmote || isEmoting || currentIngredientBox != null || currentDrinkMachine != null || currentChiliPot != null)
    {
        direction = Vector3.zero;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        return;
    }

    bool hasMovementInput = direction.sqrMagnitude > 0.01f;
    bool wantsToSprint = Input.GetKey(Run);
    bool canSprint = wantsToSprint && hasMovementInput && !sprintOnCooldown && sprintTimer > 0f;

    if (sprintDust != null)
    {
        if (canSprint && !sprintDust.isPlaying)
            sprintDust.Play();
        else if (!canSprint && sprintDust.isPlaying)
            sprintDust.Stop();
    }

    float currentMoveSpeed = canSprint ? sprintSpeed : walkSpeed;

    if (canSprint)
    {
        sprintTimer -= Time.fixedDeltaTime;

        if (sprintTimer <= 0f)
        {
            sprintTimer = 0f;
            sprintOnCooldown = true;
            sprintCooldownTimer = sprintCooldown;
        }
    }

    if (sprintOnCooldown)
    {
        sprintCooldownTimer -= Time.fixedDeltaTime;

        if (sprintCooldownTimer <= 0f)
        {
            sprintCooldownTimer = 0f;
            sprintTimer = sprintDuration;
            sprintOnCooldown = false;
        }
    }

    Vector3 moveVelocity = direction.normalized * currentMoveSpeed;
    rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);

    if (hasMovementInput)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmooth * Time.fixedDeltaTime
        );
    }
}

    private void FaceEmoteTextToCamera()
    {
        if (emoteSelectionObject == null || !emoteSelectionObject.activeInHierarchy)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform t = emoteSelectionObject.transform;

        Vector3 direction = cam.transform.position - t.position;
        t.rotation = Quaternion.LookRotation(direction);
        t.Rotate(0f, 180f, 0f);
    }

    private void HandleEmotes()
    {
        if (animator == null)
            return;

        if (isSelectingEmote)
            return;

        if (isEmoting)
            return;

        if (IsMoving())
            return;
    }

    private void HandleEmoteSelectionInput()
    {
        KeyCode selectKey = playerNumber == 1 ? p1SelectKey : p2SelectKey;

        if (Input.GetKeyDown(selectKey))
        {
            if (!isSelectingEmote)
            {
                StartEmoteSelection();
                return;
            }

            ConfirmEmoteSelection();
            return;
        }

        if (!isSelectingEmote)
            return;

        if (Input.GetKeyDown(MoveLeft))
            SelectPreviousEmote();
        else if (Input.GetKeyDown(MoveRight))
            SelectNextEmote();

        direction = Vector3.zero;
    }

    private void StartEmoteSelection()
    {
        if (emoteSelectionLabels == null || emoteSelectionLabels.Length == 0)
            return;

        isSelectingEmote = true;
        selectedEmoteIndex = 0;
        ShowEmoteSelectionText(true);
        UpdateEmoteSelectionText();
    }

    private void ConfirmEmoteSelection()
    {
        isSelectingEmote = false;
        ShowEmoteSelectionText(false);
        isEmoting = true;

        switch (selectedEmoteIndex)
        {
            case 0: PlayEmote(emote1Hash); break;
            case 1: PlayEmote(emote2Hash); break;
            case 2: PlayEmote(emote3Hash); break;
            case 3: PlayEmote(emote4Hash); break;
            default: break;
        }
    }

    private void SelectPreviousEmote()
    {
        if (emoteSelectionLabels == null || emoteSelectionLabels.Length == 0)
            return;

        selectedEmoteIndex = (selectedEmoteIndex - 1 + emoteSelectionLabels.Length) % emoteSelectionLabels.Length;
        UpdateEmoteSelectionText();
    }

    private void SelectNextEmote()
    {
        if (emoteSelectionLabels == null || emoteSelectionLabels.Length == 0)
            return;

        selectedEmoteIndex = (selectedEmoteIndex + 1) % emoteSelectionLabels.Length;
        UpdateEmoteSelectionText();
    }

    private void CreateEmoteSelectionText()
    {
        if (emoteSelectionText != null)
            emoteSelectionObject = emoteSelectionText.gameObject;

        if (emoteSelectionText == null)
        {
            emoteSelectionText = GetComponentInChildren<TextMesh>();
            if (emoteSelectionText != null)
                emoteSelectionObject = emoteSelectionText.gameObject;
        }

        if (emoteSelectionText != null)
        {
            emoteSelectionObject = emoteSelectionText.gameObject;
            emoteSelectionText.text = string.Empty;
        }

        if (emoteSelectionObject != null)
            emoteSelectionObject.SetActive(false);
    }

    private void UpdateEmoteSelectionText()
    {
        if (emoteSelectionText == null || emoteSelectionLabels == null || emoteSelectionLabels.Length == 0)
            return;

        emoteSelectionText.text = emoteSelectionLabels[selectedEmoteIndex];
    }

    private void ShowEmoteSelectionText(bool show)
    {
        if (emoteSelectionObject != null)
            emoteSelectionObject.SetActive(show);
    }

    private void PlayEmote(int emoteHash)
    {
        animator.ResetTrigger(emote1Hash);
        animator.ResetTrigger(emote2Hash);
        animator.ResetTrigger(emote3Hash);
        animator.ResetTrigger(emote4Hash);

        rb.linearVelocity = Vector3.zero;
        animator.SetTrigger(emoteHash);
        StartCoroutine(WaitForEmoteToEnd());
    }

    private System.Collections.IEnumerator WaitForEmoteToEnd()
    {
        yield return new WaitForSeconds(emoteLockDuration);
        isEmoting = false;
    }

    private void EnsureMovementState()
    {
        if (!isSelectingEmote && !isEmoting && currentIngredientBox == null && currentDrinkMachine == null && currentChiliPot == null)
        {
            doMove = true;
            Debug.Log("PlayerControl: movement lock cleared automatically.");
        }
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
        droppedRigidbody.useGravity = false; // Disable initially to let item reach peak first
        droppedRigidbody.linearDamping = 4f;
        droppedRigidbody.angularDamping = 4f;
        droppedRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        droppedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        droppedRigidbody.AddForce(velocity, ForceMode.VelocityChange);

        DroppedItem droppedItem = droppedObject.AddComponent<DroppedItem>();
        droppedItem.Initialize(itemData, heldItemVisualizer);
        droppedItem.SetWaitForPeak(); // Enable gravity once item reaches peak

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
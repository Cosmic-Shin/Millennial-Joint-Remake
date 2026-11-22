using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum ControllerAxisSource
    {
        LSB,
        RSB,
        DPad
    }

public enum ControlScheme
{
    Keyboard,
    Joystick
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
    [Header("Player")]
    public int playerNumber = 1;

    [Header("Control Scheme")]
    public ControlScheme controlScheme = ControlScheme.Keyboard;
    public int joystickNumber = 1;
    public bool arrowsLayout;

    [Header("Movement")]
    public bool doMove = true;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSmooth = 14f;
    public float acceleration = 70f;
    public float deceleration = 90f;

    [Header("Input Forgiveness")]
    public float interactBufferTime = 0.15f;

    [Header("Model Feel")]
    public float leanAngle = 9f;
    public float turnRollAngle = 7f;
    public float leanSmooth = 12f;
    public float turnDustAngle = 120f;
    public Color dustColor = new Color(0.85f, 0.8f, 0.72f, 0.8f);

    [Header("Sprint Stamina")]
    public float sprintDuration = 3f;
    public float staminaRecoveryRate = 1.5f;
    [Tooltip("After running out, sprinting unlocks again at this fraction of stamina")]
    [Range(0f, 1f)] public float staminaResumeFraction = 0.3f;

    [Header("Sprint Effects")]
    public ParticleSystem sprintDust;

    [Header("Sprint Audio")]
    public AudioSource sprintAudioSource;
    public AudioClip sprintClip;
    [Range(0f, 1f)] public float sprintVolume = 1f;

    private float sprintTimer;
    private bool sprintOnCooldown;
    private bool isCurrentlySprinting;

    public float StaminaFraction => sprintTimer / Mathf.Max(0.01f, sprintDuration);
    public bool IsExhausted => sprintOnCooldown;
    public bool HasUnlimitedStamina => MatchRules.UnlimitedStamina ||
        (PowerUpManager.Instance != null && PowerUpManager.Instance.HasUnlimitedStamina(playerNumber));

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

    [Header("Carrying Slowdown")]
    [SerializeField] private float carryingSpeedMultiplier = 0.75f;

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
    [Tooltip("How strongly targeting prefers stations in front of the chef (metres of distance forgiven)")]
    public float facingBias = 0.6f;

    // Distance minus a bonus for being in front: a station ahead beats a slightly nearer one behind.
    private float FacingScore(Vector3 point)
    {
        Vector3 to = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
        float facing = to.sqrMagnitude > 0.0001f ? Vector3.Dot(transform.forward, to.normalized) : 1f;
        return to.magnitude - facingBias * facing;
    }
    public float droppedItemLifetime = 30f;

    private Rigidbody rb;
    private Vector3 direction;
    private bool front, left, back, right;

    private bool dropButtonHeld;
    private float dropButtonHoldTime;
    private float previousPadHorizontal;
    private BaseStation lastOpenStation;

    private int isMovingHash;
    private int emote1Hash;
    private int emote2Hash;
    private int emote3Hash;
    private int emote4Hash;

    public ChiliPotCounter currentChiliPot;
    public CoffeeMachine currentCoffeeMachine;

    private bool isSelectingEmote;
    private bool isEmoting;
    private int selectedEmoteIndex;

    public GameObject emoteSelectionObject;
    public IngredientBox currentIngredientBox;
    public DrinkMachine currentDrinkMachine;
    public IceCreamMachine currentIceCreamMachine;
    public DonutGlazeStation currentGlazeStation;

    [Header("Joystick Mapping")]
    [FormerlySerializedAs("p2MovementAxis")] public ControllerAxisSource padMovementAxis = ControllerAxisSource.LSB;
    [FormerlySerializedAs("p2InteractKey")] public KeyCode padInteractKey = KeyCode.Joystick1Button0;
    [FormerlySerializedAs("p2RunKey")] public KeyCode padRunKey = KeyCode.Joystick1Button1;
    [FormerlySerializedAs("p2EmoteKey")] public KeyCode padEmoteKey = KeyCode.Joystick1Button3;
    [FormerlySerializedAs("p2ThrowKey")] public KeyCode padThrowKey = KeyCode.Joystick1Button2;

    public bool UsesJoystick => controlScheme == ControlScheme.Joystick;

    public KeyCode EmoteKey
    {
        get => playerNumber == 1 ? p1SelectKey : p2SelectKey;
        set { if (playerNumber == 1) p1SelectKey = value; else p2SelectKey = value; }
    }

    [System.Serializable]
    public struct InputBindings
    {
        public ControlScheme scheme;
        public int joystickNumber;
        public bool arrows;
        public KeyCode up, left, down, right, interact, run, drop, emote;
        public ControllerAxisSource padAxis;
        public KeyCode padInteract, padRun, padEmote, padThrow;
    }

    public InputBindings Bindings
    {
        get => new InputBindings
        {
            scheme = controlScheme, joystickNumber = joystickNumber, arrows = arrowsLayout,
            up = MoveUp, left = MoveLeft, down = MoveDown, right = MoveRight,
            interact = PrimaryAction, run = Run, drop = DropItem, emote = EmoteKey,
            padAxis = padMovementAxis,
            padInteract = padInteractKey, padRun = padRunKey, padEmote = padEmoteKey, padThrow = padThrowKey
        };
        set
        {
            controlScheme = value.scheme; joystickNumber = value.joystickNumber; arrowsLayout = value.arrows;
            MoveUp = value.up; MoveLeft = value.left; MoveDown = value.down; MoveRight = value.right;
            PrimaryAction = value.interact; Run = value.run; DropItem = value.drop; EmoteKey = value.emote;
            padMovementAxis = value.padAxis;
            padInteractKey = value.padInteract; padRunKey = value.padRun; padEmoteKey = value.padEmote; padThrowKey = value.padThrow;
        }
    }

    public void ResetBindingsToDefault()
    {
        bool wasd = !arrowsLayout;
        MoveUp = wasd ? KeyCode.W : KeyCode.UpArrow;
        MoveLeft = wasd ? KeyCode.A : KeyCode.LeftArrow;
        MoveDown = wasd ? KeyCode.S : KeyCode.DownArrow;
        MoveRight = wasd ? KeyCode.D : KeyCode.RightArrow;
        PrimaryAction = wasd ? KeyCode.F : KeyCode.Return;
        Run = wasd ? KeyCode.LeftShift : KeyCode.RightShift;
        DropItem = wasd ? KeyCode.Q : KeyCode.Slash;
        EmoteKey = wasd ? KeyCode.Z : KeyCode.Backspace;

        padMovementAxis = ControllerAxisSource.LSB;
        padInteractKey = KeyCode.Joystick1Button0;
        padRunKey = KeyCode.Joystick1Button1;
        padEmoteKey = KeyCode.Joystick1Button3;
        padThrowKey = KeyCode.Joystick1Button2;
    }

    public static void SwapKeyboardKeys(PlayerControl a, PlayerControl b)
    {
        InputBindings ka = a.Bindings, kb = b.Bindings;
        a.SetKeyboardKeys(kb);
        b.SetKeyboardKeys(ka);
    }

    private void SetKeyboardKeys(InputBindings k)
    {
        arrowsLayout = k.arrows;
        MoveUp = k.up; MoveLeft = k.left; MoveDown = k.down; MoveRight = k.right;
        PrimaryAction = k.interact; Run = k.run; DropItem = k.drop; EmoteKey = k.emote;
    }

    public static string AxisName(int joystick, ControllerAxisSource source, bool horizontal)
    {
        string prefix = source switch
        {
            ControllerAxisSource.RSB => "RightStick",
            ControllerAxisSource.DPad => "DPad",
            _ => ""
        };
        return $"Joy{joystick}{prefix}{(horizontal ? "Horizontal" : "Vertical")}";
    }

    private float PadAxis(bool horizontal)
    {
        return Input.GetAxisRaw(AxisName(joystickNumber, padMovementAxis, horizontal));
    }

    private KeyCode PadKey(KeyCode key)
    {
        int offset = key - KeyCode.JoystickButton0;
        if (offset < 0)
            return key;

        return KeyCode.JoystickButton0 + joystickNumber * 20 + offset % 20;
    }

    private void OnValidate()
    {
        if (hitMask.value == 0)
            hitMask = ~0;
    }

    private void Awake()
    {
        arrowsLayout = playerNumber == 2;
        isMovingHash = Animator.StringToHash(isMovingParameter);
        emote1Hash = Animator.StringToHash(emote1Trigger);
        emote2Hash = Animator.StringToHash(emote2Trigger);
        emote3Hash = Animator.StringToHash(emote3Trigger);
        emote4Hash = Animator.StringToHash(emote4Trigger);
    }

    private void Start()
    {
        sprintTimer = sprintDuration;
        sprintOnCooldown = false;
        isCurrentlySprinting = false;

        if (sprintDust != null)
        {
            sprintDust.Stop();
            VisualFx.FxMaterial = sprintDust.GetComponent<ParticleSystemRenderer>().sharedMaterial;
        }

        SetupSprintAudio();

        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            modelBaseScale = animator.transform.localScale;
            modelBaseRotation = animator.transform.localRotation;
            modelBasePosition = animator.transform.localPosition;
        }

        lastYaw = transform.eulerAngles.y;

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

    private void SetupSprintAudio()
    {
        if (sprintAudioSource == null)
            sprintAudioSource = gameObject.AddComponent<AudioSource>();

        sprintAudioSource.clip = sprintClip;
        sprintAudioSource.loop = true;
        sprintAudioSource.playOnAwake = false;
        sprintAudioSource.volume = sprintVolume;
    }

    private void Update()
    {
        if (rb == null)
            return;

        if (!GameplayActive || inputLocked)
        {
            direction = Vector3.zero;
            UpdateAnimator();
            SetHighlightTarget(null);
            return;
        }

        if (frozenTimer > 0f)
            frozenTimer -= Time.deltaTime;

        UpdateFrozenVisual();

        EnsureMovementState();

        bool wasLocked = IsMovementLocked();

        front = IsMoveUpHeld();
        left = IsMoveLeftHeld();
        back = IsMoveDownHeld();
        right = IsMoveRightHeld();

        if (!IsMovementLocked() && UsesJoystick)
        {
            // Analog stick: full 360° steering, gentle tilt = slower walk.
            Vector3 stick = new Vector3(PadAxis(true), 0f, PadAxis(false));
            direction = stick.magnitude > 0.2f ? Vector3.ClampMagnitude(stick, 1f) : Vector3.zero;
        }
        else if (!IsMovementLocked())
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

        if (reversedTimer > 0f)
        {
            reversedTimer -= Time.deltaTime;
            direction = -direction;

            // Pulsing purple while reversed, so both players can see why this chef is stumbling.
            if (!IsFrozen && nopeTimer <= 0f)
            {
                if (reversedTimer > 0f)
                    VisualFx.Tint(gameObject, reversedTint, 0.35f + 0.15f * Mathf.Sin(Time.time * 8f));
                else
                    VisualFx.ClearTint(gameObject);
            }
        }

        HandleEmoteSelectionInput();

        if (currentIngredientBox != null)
            currentIngredientBox.HandleIngredientSelectionInput();

        bool interactPressed = IsPrimaryActionPressed();
        interactBufferTimer = interactPressed ? interactBufferTime : interactBufferTimer - Time.deltaTime;

        if (interactPressed || (interactBufferTimer > 0f && !IsMovementLocked() && FindInteractTarget() != null))
        {
            if (FindInteractTarget() != null)
                interactBufferTimer = 0f;

            HandleInteraction();
        }

        if (currentDrinkMachine != null)
            currentDrinkMachine.HandleDrinkSelectionInput();

        if (currentIceCreamMachine != null)
            currentIceCreamMachine.HandleIceCreamSelectionInput();

        if (currentGlazeStation != null)
            currentGlazeStation.HandleGlazeSelectionInput();

        HandleDropInput(wasLocked);

        IInteractable target = FindInteractTarget();
        SetHighlightTarget(target is MonoBehaviour mb ? mb.gameObject : null);
        VisualFx.PulseHighlight(highlightTarget);

        UpdateAnimator();
        FaceEmoteTextToCamera();

        if (UsesJoystick)
            previousPadHorizontal = PadAxis(true);
    }

    private void FixedUpdate()
    {
        if (rb != null && !GameplayActive)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            moveVelocity = Vector3.zero;
        }

        // Turn to face the station just used (also while busy pouring/selecting); any stick input cancels it.
        if (direction.sqrMagnitude > 0.01f)
            faceDirection = Vector3.zero;
        else if (rb != null && faceDirection != Vector3.zero && GameplayActive)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(faceDirection), 1f - Mathf.Exp(-18f * Time.fixedDeltaTime)));

        if (!doMove || rb == null || !GameplayActive)
        {
            StopSprintAudio();
            if (GameplayActive)
                RecoverStamina();
            return;
        }

        if (IsMovementLocked())
        {
            direction = Vector3.zero;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            moveVelocity = Vector3.zero;
            StopSprintAudio();
            StopSprintDust();
            RecoverStamina();
            return;
        }

        bool hasMovementInput = direction.sqrMagnitude > 0.01f;
        bool wantsToSprint = IsRunHeld();
        bool hasUnlimitedStamina = HasUnlimitedStamina;
        bool canSprint = wantsToSprint && hasMovementInput && (hasUnlimitedStamina || (!sprintOnCooldown && sprintTimer > 0f));

        HandleSprintEffects(canSprint);

        float currentMoveSpeed = (canSprint ? sprintSpeed : walkSpeed) * MatchRules.MoveSpeedMultiplier;

        if (!heldItem.IsEmpty)
            currentMoveSpeed *= carryingSpeedMultiplier;

        if (canSprint && !hasUnlimitedStamina)
        {
            sprintTimer -= Time.fixedDeltaTime;

            if (sprintTimer <= 0f)
            {
                sprintTimer = 0f;
                sprintOnCooldown = true;
                StopSprintAudio();
                StopSprintDust();
            }
        }
        else
        {
            RecoverStamina();
        }

        Vector3 targetVelocity = Vector3.ClampMagnitude(direction, 1f) * currentMoveSpeed;
        float rate = (hasMovementInput ? acceleration : deceleration) * (heldItem.IsEmpty ? 1f : carryingSpeedMultiplier);
        moveVelocity = Vector3.MoveTowards(moveVelocity, targetVelocity, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);

        if (hasMovementInput)
        {
            // MoveRotation keeps Rigidbody interpolation; writing transform.rotation here made turns jitter.
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                1f - Mathf.Exp(-rotationSmooth * Time.fixedDeltaTime)
            ));
        }
    }

    private void RecoverStamina()
    {
        sprintTimer = Mathf.Min(sprintTimer + staminaRecoveryRate * Time.fixedDeltaTime, sprintDuration);

        if (sprintOnCooldown && sprintTimer >= sprintDuration * staminaResumeFraction)
            sprintOnCooldown = false;
    }

    public static bool GameplayActive =>
        OrderManager.Instance != null && OrderManager.Instance.IsRunning && Time.timeScale > 0f;

    public bool CanMove => GameplayActive && !IsMovementLocked();

    [Header("Visual Feedback")]
    public Sprite freezeIcon;
    public Color frozenTint = new Color(0.45f, 0.75f, 1f);
    public Color infoTextColor = new Color(0.1f, 0.1f, 0.1f);
    public Color errorTextColor = new Color(0.85f, 0.1f, 0.1f);

    private GameObject freezeIconObject;
    private bool frozenVisualOn;
    private GameObject highlightTarget;
    private GameObject floatingText;
    private Coroutine squashRoutine;
    private Vector3 modelBaseScale = Vector3.one;

    private float frozenTimer;
    public bool IsFrozen => frozenTimer > 0f;

    public void Freeze(float seconds)
    {
        frozenTimer = Mathf.Max(frozenTimer, seconds);
    }

    public Color reversedTint = new Color(0.7f, 0.3f, 1f);
    private float reversedTimer;
    public bool IsReversed => reversedTimer > 0f;

    public void OnCaught(Vector3 from)
    {
        RefreshHeldItemDisplay();
        Transform held = heldItemVisualizer != null ? heldItemVisualizer.CurrentVisual : null;
        if (held != null)
            heldFlyOffset = from - held.position;
        PlaySquash(0.22f);
        ShowFloatingText("Nice catch!", new Color(0.2f, 0.7f, 0.25f));
        VisualFx.Puff(from, new Color(2.2f, 1.8f, 0.6f), 10, 0.15f, 2.5f);
    }

    public void ReverseControls(float seconds)
    {
        reversedTimer = Mathf.Max(reversedTimer, seconds);
        ShowFloatingText("Controls reversed!", reversedTint);
        PlaySquash(0.25f);
        VisualFx.Puff(transform.position + Vector3.up * 1.2f, reversedTint, 12, 0.3f, 1.5f);
    }

    private void UpdateFrozenVisual()
    {
        if (IsFrozen != frozenVisualOn)
        {
            frozenVisualOn = IsFrozen;

            if (frozenVisualOn)
                VisualFx.Tint(gameObject, frozenTint, 0.65f);
            else
                VisualFx.ClearTint(gameObject);

            if (frozenVisualOn && freezeIconObject == null && freezeIcon != null)
            {
                freezeIconObject = new GameObject("FreezeIcon");
                freezeIconObject.transform.SetParent(transform, false);
                freezeIconObject.transform.localPosition = Vector3.up * 2.4f;
                freezeIconObject.transform.localScale = Vector3.one * 0.3f;
                var sr = freezeIconObject.AddComponent<SpriteRenderer>();
                sr.sprite = freezeIcon;
                sr.color = new Color(0.75f, 0.95f, 1f);
            }

            if (freezeIconObject != null)
                freezeIconObject.SetActive(frozenVisualOn);
        }

        if (frozenVisualOn && freezeIconObject != null && Camera.main != null)
            freezeIconObject.transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(0f, 0f, Time.time * 90f);
    }

    private void SetHighlightTarget(GameObject target)
    {
        if (target == highlightTarget)
            return;

        VisualFx.Highlight(highlightTarget, false);
        highlightTarget = target;
        VisualFx.Highlight(highlightTarget, true);
    }

    // Big squash for landing from a pad hop or dropping back in after a fall.
    public void PlayLanding(float amount = 0.3f) => PlaySquash(amount);

    private void PlaySquash(float amount = 0.2f)
    {
        if (animator == null)
            return;

        if (squashRoutine != null)
            StopCoroutine(squashRoutine);

        squashRoutine = StartCoroutine(SquashRoutine(animator.transform, amount));
    }

    private System.Collections.IEnumerator SquashRoutine(Transform model, float amount)
    {
        yield return VisualFx.Squash(model, modelBaseScale, amount, 0.3f);
        squashRoutine = null;
    }

    public bool IsChargingThrow => dropButtonHeld && !heldItem.IsEmpty && dropButtonHoldTime >= minimumThrowHoldTime;

    public float ThrowCharge => IsChargingThrow
        ? Mathf.Clamp01((dropButtonHoldTime - minimumThrowHoldTime) / Mathf.Max(0.0001f, maximumThrowHoldTime - minimumThrowHoldTime))
        : 0f;

    private const float NopeDuration = 0.35f;
    private float nopeTimer;

    public void PlayNope()
    {
        nopeTimer = NopeDuration;
        AudioManager.Instance?.PlayNopeSFX();
    }

    private void LateUpdate()
    {
        if (animator == null || rb == null)
            return;

        Transform model = animator.transform;
        float dt = Time.deltaTime;

        float speedFraction = Mathf.Clamp01(moveVelocity.magnitude / Mathf.Max(0.01f, sprintSpeed));

        float yaw = transform.eulerAngles.y;
        float turnRate = dt > 0f ? Mathf.DeltaAngle(lastYaw, yaw) / dt : 0f;
        lastYaw = yaw;

        float smoothing = 1f - Mathf.Exp(-leanSmooth * dt);
        currentLean = Mathf.Lerp(currentLean, leanAngle * speedFraction, smoothing);
        currentRoll = Mathf.Lerp(currentRoll, -Mathf.Clamp(turnRate / 600f, -1f, 1f) * turnRollAngle * speedFraction, smoothing);

        float nopeYaw = 0f;
        if (nopeTimer > 0f)
        {
            nopeTimer = Mathf.Max(0f, nopeTimer - dt);
            float k = nopeTimer / NopeDuration;
            nopeYaw = Mathf.Sin(nopeTimer * 45f) * 16f * k;

            if (!IsFrozen)
            {
                if (nopeTimer > 0f)
                    VisualFx.Tint(gameObject, errorTextColor, 0.45f * k);
                else
                    VisualFx.ClearTint(gameObject);
            }
        }

        model.localRotation = modelBaseRotation * Quaternion.Euler(currentLean, nopeYaw, currentRoll);

        if (squashRoutine == null)
        {
            float charge = IsChargingThrow ? 0.25f + 0.75f * ThrowCharge : 0f;
            model.localScale = Vector3.Scale(modelBaseScale, new Vector3(1f + 0.1f * charge, 1f - 0.16f * charge, 1f + 0.1f * charge));
        }

        // One step phase drives the body hop, the food bob and footstep dust so they stay in sync.
        int stepBefore = Mathf.FloorToInt(carryBob / Mathf.PI);
        carryBob += dt * 14f * speedFraction;
        float hop = Mathf.Abs(Mathf.Sin(carryBob)) * speedFraction;
        model.localPosition = modelBasePosition + Vector3.up * (stepHop * hop);

        if (Mathf.FloorToInt(carryBob / Mathf.PI) != stepBefore && speedFraction > 0.35f)
            VisualFx.Puff(transform.position + Vector3.up * 0.05f, dustColor, isCurrentlySprinting ? 3 : 2, 0.2f, 0.6f);

        Transform held = heldItemVisualizer != null ? heldItemVisualizer.CurrentVisual : null;
        if (held != null)
        {
            held.localPosition = heldItemVisualizer.localPosition + Vector3.up * (0.07f * hop);
            // Fresh pickups start at the station and swoop into the hands.
            held.position += heldFlyOffset + Vector3.up * (heldFlyOffset.magnitude * 0.3f);
        }
        heldFlyOffset *= Mathf.Exp(-16f * dt);

        bool moving = direction.sqrMagnitude > 0.01f;
        if (moving != wasMovingInput)
        {
            wasMovingInput = moving;
            if (GameplayActive && !IsChargingThrow)
                PlaySquash(moving ? -0.1f : 0.12f);
        }

        if (moving)
        {
            if (lastMoveDirection != Vector3.zero && speedFraction > 0.3f && Vector3.Angle(lastMoveDirection, direction) >= turnDustAngle)
                VisualFx.Puff(transform.position + Vector3.up * 0.1f, dustColor, 7, 0.35f);

            lastMoveDirection = direction;
        }
    }

    private Quaternion modelBaseRotation = Quaternion.identity;
    private float lastYaw;
    private float currentLean;
    private float currentRoll;
    private float carryBob;
    private Vector3 heldFlyOffset;
    private Vector3 faceDirection;
    private Vector3 modelBasePosition;
    [Tooltip("How high the chef hops with each step at full speed")]
    public float stepHop = 0.08f;
    private bool wasMovingInput;
    private Vector3 lastMoveDirection;
    private float interactBufferTimer;
    private Vector3 moveVelocity;

    public void ShowFloatingText(string message, Color color)
    {
        if (emoteSelectionObject == null || string.IsNullOrEmpty(message))
            return;

        if (floatingText != null)
            Destroy(floatingText);

        floatingText = Instantiate(emoteSelectionObject, emoteSelectionObject.transform.parent);
        floatingText.name = "FloatingText";
        floatingText.transform.localPosition = emoteSelectionObject.transform.localPosition + Vector3.up * 0.45f;
        floatingText.transform.localScale = emoteSelectionObject.transform.localScale * 0.6f;
        floatingText.SetActive(true);

        TextMesh text = floatingText.GetComponent<TextMesh>();
        if (text == null)
        {
            Destroy(floatingText);
            return;
        }

        text.text = message;
        text.color = color;
        StartCoroutine(FloatAway(floatingText, text));
    }

    private System.Collections.IEnumerator FloatAway(GameObject go, TextMesh text)
    {
        const float duration = 1.3f;
        Color color = text.color;

        for (float t = 0f; t < duration && go != null; t += Time.deltaTime)
        {
            go.transform.localPosition += Vector3.up * 0.5f * Time.deltaTime;

            if (Camera.main != null)
                go.transform.rotation = Quaternion.LookRotation(go.transform.position - Camera.main.transform.position);

            color.a = t < duration * 0.5f ? 1f : 1f - (t - duration * 0.5f) / (duration * 0.5f);
            text.color = color;
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    private bool IsMovementLocked()
    {
        return IsFrozen ||
               isSelectingEmote ||
               isEmoting ||
               currentIngredientBox != null ||
               currentDrinkMachine != null ||
               currentChiliPot != null ||
               currentCoffeeMachine != null ||
               currentIceCreamMachine != null ||
               currentGlazeStation != null;
    }

    private void HandleSprintEffects(bool canSprint)
    {
        if (canSprint)
        {
            StartSprintAudio();

            if (sprintDust != null && !sprintDust.isPlaying)
                sprintDust.Play();
        }
        else
        {
            StopSprintAudio();
            StopSprintDust();
        }
        SetSpeedTrail(canSprint);
    }

    private TrailRenderer speedTrail;

    // A streak behind a sprinting chef, in the player's ring colour.
    private void SetSpeedTrail(bool on)
    {
        if (speedTrail == null)
        {
            if (!on || VisualFx.FxMaterial == null)
                return;

            var go = new GameObject("Speed Trail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.45f;
            speedTrail = go.AddComponent<TrailRenderer>();
            speedTrail.sharedMaterial = VisualFx.FxMaterial;
            speedTrail.time = 0.3f;
            speedTrail.minVertexDistance = 0.1f;
            speedTrail.widthCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0f);
            speedTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            StaminaRing ring = GetComponentInChildren<StaminaRing>(true);
            MeshRenderer ringRenderer = ring != null ? ring.GetComponent<MeshRenderer>() : null;
            Color c = ringRenderer != null ? ringRenderer.sharedMaterial.GetColor("_BaseColor") : Color.white;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            speedTrail.colorGradient = fade;
        }
        speedTrail.emitting = on;
    }

    private void StartSprintAudio()
    {
        if (isCurrentlySprinting)
            return;

        if (sprintAudioSource == null || sprintClip == null)
            return;

        sprintAudioSource.clip = sprintClip;
        sprintAudioSource.loop = true;
        sprintAudioSource.volume = sprintVolume;
        sprintAudioSource.Play();

        isCurrentlySprinting = true;
    }

    private void StopSprintAudio()
    {
        if (!isCurrentlySprinting)
            return;

        if (sprintAudioSource != null)
            sprintAudioSource.Stop();

        isCurrentlySprinting = false;
    }

    private void StopSprintDust()
    {
        if (sprintDust != null && sprintDust.isPlaying)
            sprintDust.Stop();
    }

    private IInteractable FindInteractTarget()
    {
        IInteractable interactable = null;

        if (heldItem.IsEmpty)
            interactable = GetNearestValidDroppedItem(pickupRadius, skipOwnFreshDrops: true);

        if (interactable == null)
        {
            interactable = GetNearestValidInteractable(castRadius);

            if (interactable == null && heldItem.IsEmpty)
                interactable = GetNearestValidInteractable(pickupRadius);
        }

        if (interactable == null && heldItem.IsEmpty)
            interactable = GetNearestValidDroppedItem(pickupRadius);

        return interactable;
    }

    private void HandleInteraction()
    {
        IInteractable interactable = FindInteractTarget();
        ItemType heldBefore = heldItem.type;

        if (interactable == null)
        {
            IInteractable blocked = GetNearestValidInteractable(castRadius, requireValid: false);
            if (blocked != null)
            {
                ShowFloatingText(blocked is BaseStation { IsLocked: true } ? "Locked!"
                    : heldItem.IsEmpty ? "Can't use that right now" : "Can't use " + heldItem.GetDisplayName() + " here", errorTextColor);
                PlayNope();
            }
        }

        if (interactable != null)
        {
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

            var source = interactable as MonoBehaviour;
            Vector3 sourcePos = source != null ? source.transform.position + Vector3.up * 0.9f : transform.position;
            Transform heldBeforeVisual = heldItemVisualizer != null ? heldItemVisualizer.CurrentVisual : null;
            Vector3 handPos = heldBeforeVisual != null ? heldBeforeVisual.position : transform.position + Vector3.up;

            if (source != null)
            {
                Vector3 to = Vector3.ProjectOnPlane(source.transform.position - transform.position, Vector3.up);
                if (to.sqrMagnitude > 0.01f)
                    faceDirection = to.normalized;
            }

            interactable.Interact(this);
            UpdateHeldItemHUD();
            RefreshHeldItemVisual();

            if (heldItem.type != heldBefore)
            {
                PlaySquash();
                (interactable as BaseStation)?.Bounce();

                // Items travel between hands and station instead of teleporting.
                Transform held = heldItemVisualizer != null ? heldItemVisualizer.CurrentVisual : null;
                if (heldBefore == ItemType.None && held != null)
                    heldFlyOffset = sourcePos - held.position;
                else if (heldItem.IsEmpty && source != null)
                    source.GetComponentInChildren<KitchenItemVisualizer>()?.FlyFrom(handPos);
            }

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
        }
    }

    private void HandleDropInput(bool movementLocked)
    {
        if (!movementLocked && IsDropItemPressed() && !heldItem.IsEmpty)
        {
            dropButtonHeld = true;
            dropButtonHoldTime = 0f;
        }

        if (IsDropItemHeld() && dropButtonHeld)
        {
            dropButtonHoldTime += Time.deltaTime;
        }

        if (IsDropItemReleased() && dropButtonHeld)
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
                    // Half the run speed carries into the throw: lob on the move to reach further.
                    velocity = transform.forward * strength + Vector3.up * throwUpwardBoost + moveVelocity * 0.5f;
                    AudioManager.Instance?.PlayThrowSFX();

                    // Release snap: stretch forward, kick of dust, tiny shake on a full-power throw.
                    PlaySquash(-0.15f - 0.15f * charge);
                    VisualFx.Puff(transform.position + Vector3.up * 0.1f - transform.forward * 0.3f, dustColor, 4 + Mathf.RoundToInt(6 * charge), 0.3f);
                    if (charge > 0.9f)
                        CameraMovement.Instance?.Shake(0.12f, 0.15f);
                }
                else
                {
                    velocity = Vector3.up * dropUpwardVelocity;
                }

                SpawnDroppedItem(heldItem, dropPosition, velocity);
                heldItem.Clear();
                UpdateHeldItemHUD();
                RefreshHeldItemVisual();
                PlaySquash(isThrow ? -0.28f : 0.2f);
            }

            dropButtonHeld = false;
            dropButtonHoldTime = 0f;
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

        Vector3 directionToCamera = cam.transform.position - t.position;
        t.rotation = Quaternion.LookRotation(directionToCamera);
        t.Rotate(0f, 180f, 0f);
    }

    private void HandleEmoteSelectionInput()
    {
        if (IsEmoteSelectPressed())
        {
            if (!isSelectingEmote)
            {
                if (IsMovementLocked())
                    return;

                StartEmoteSelection();
                return;
            }

            ConfirmEmoteSelection();
            return;
        }

        if (!isSelectingEmote)
            return;

        // Drop/throw key backs out without dancing.
        if (IsDropItemPressed())
        {
            isSelectingEmote = false;
            ShowEmoteSelectionText(false);
            direction = Vector3.zero;
            return;
        }

        if (IsMoveLeftPressed())
            SelectPreviousEmote();
        else if (IsMoveRightPressed())
            SelectNextEmote();

        direction = Vector3.zero;
    }

    public bool IsMoveUpHeld()
    {
        return UsesJoystick ? PadAxis(false) > 0.2f : Input.GetKey(MoveUp);
    }

    public bool IsMoveDownHeld()
    {
        return UsesJoystick ? PadAxis(false) < -0.2f : Input.GetKey(MoveDown);
    }

    public bool IsMoveLeftHeld()
    {
        return UsesJoystick ? PadAxis(true) < -0.2f : Input.GetKey(MoveLeft);
    }

    public bool IsMoveRightHeld()
    {
        return UsesJoystick ? PadAxis(true) > 0.2f : Input.GetKey(MoveRight);
    }

    public bool IsMoveLeftPressed()
    {
        return UsesJoystick
            ? PadAxis(true) < -0.5f && previousPadHorizontal >= -0.5f
            : Input.GetKeyDown(MoveLeft);
    }

    public bool IsMoveRightPressed()
    {
        return UsesJoystick
            ? PadAxis(true) > 0.5f && previousPadHorizontal <= 0.5f
            : Input.GetKeyDown(MoveRight);
    }

    private const float MenuRepeatDelay = 0.35f;
    private const float MenuRepeatRate = 0.12f;
    private int menuHeldDir;
    private float menuRepeatTimer;

    public int GetMenuStep()
    {
        int dir = IsMoveLeftHeld() ? -1 : IsMoveRightHeld() ? 1 : 0;

        if (dir == 0)
        {
            menuHeldDir = 0;
            return 0;
        }

        if (dir != menuHeldDir)
        {
            menuHeldDir = dir;
            menuRepeatTimer = MenuRepeatDelay;
            return dir;
        }

        menuRepeatTimer -= Time.deltaTime;
        if (menuRepeatTimer > 0f)
            return 0;

        menuRepeatTimer = MenuRepeatRate;
        return dir;
    }

    public void ResetMenuStep()
    {
        menuHeldDir = IsMoveLeftHeld() ? -1 : IsMoveRightHeld() ? 1 : 0;
        menuRepeatTimer = float.MaxValue;
    }

    public bool IsPrimaryActionPressed()
    {
        return Input.GetKeyDown(UsesJoystick ? PadKey(padInteractKey) : PrimaryAction);
    }

    public bool IsRunHeld()
    {
        return Input.GetKey(UsesJoystick ? PadKey(padRunKey) : Run);
    }

    public bool IsDropItemPressed()
    {
        return Input.GetKeyDown(UsesJoystick ? PadKey(padThrowKey) : DropItem);
    }

    public bool IsDropItemHeld()
    {
        return Input.GetKey(UsesJoystick ? PadKey(padThrowKey) : DropItem);
    }

    public bool IsDropItemReleased()
    {
        return Input.GetKeyUp(UsesJoystick ? PadKey(padThrowKey) : DropItem);
    }

    public bool IsEmoteSelectPressed()
    {
        return Input.GetKeyDown(UsesJoystick ? PadKey(padEmoteKey) : EmoteKey);
    }

    private void StartEmoteSelection()
    {
        if (emoteSelectionLabels == null || emoteSelectionLabels.Length == 0)
            return;

        StopSprintAudio();
        StopSprintDust();

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

        emoteSelectionText.text = "< " + emoteSelectionLabels[selectedEmoteIndex] + " >";

        // Pop each time the choice changes so flicking through feels clicky.
        if (emoteSelectionObject != null && emoteSelectionObject.activeInHierarchy)
        {
            if (emoteTextBaseScale == Vector3.zero)
                emoteTextBaseScale = emoteSelectionObject.transform.localScale;
            if (emoteTextPop != null)
                StopCoroutine(emoteTextPop);
            emoteTextPop = StartCoroutine(VisualFx.Pop(emoteSelectionObject.transform, 1.3f, 0.15f, emoteTextBaseScale));
        }
    }

    private Vector3 emoteTextBaseScale;
    private Coroutine emoteTextPop;

    private void ShowEmoteSelectionText(bool show)
    {
        if (emoteSelectionObject != null)
            emoteSelectionObject.SetActive(show);
    }

    private void PlayEmote(int emoteHash)
    {
        // No animator = nothing to wait for; without this the player stayed locked forever.
        if (animator == null)
        {
            isEmoting = false;
            return;
        }

        StopSprintAudio();
        StopSprintDust();

        animator.ResetTrigger(emote1Hash);
        animator.ResetTrigger(emote2Hash);
        animator.ResetTrigger(emote3Hash);
        animator.ResetTrigger(emote4Hash);

        rb.linearVelocity = Vector3.zero;
        moveVelocity = Vector3.zero;
        int returnState = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        animator.SetTrigger(emoteHash);

        PlaySquash(-0.2f);
        VisualFx.Puff(transform.position + Vector3.up * 0.1f, dustColor, 10, 0.35f, 1.5f);

        if (emoteRoutine != null)
            StopCoroutine(emoteRoutine);
        emoteRoutine = StartCoroutine(EmoteRoutine(returnState));
    }

    private static readonly Color[] EmoteSparkColors =
    {
        new Color(2.2f, 0.6f, 1.6f), new Color(0.5f, 1.6f, 2.2f), new Color(2.2f, 1.8f, 0.4f), new Color(0.6f, 2.2f, 0.8f)
    };

    // Lock lasts exactly as long as the dance clip (they differ: 1.6s to 6.2s), and moving cancels it.
    private System.Collections.IEnumerator EmoteRoutine(int returnState)
    {
        float t = 0f, nextSpark = 0f;
        bool entered = false;

        while (animator != null && t < emoteLockDuration * 4f)
        {
            t += Time.deltaTime;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool inEmote = state.fullPathHash != returnState || animator.IsInTransition(0);

            if (!entered)
            {
                entered = inEmote;
                if (!entered && t > 0.3f)
                    break; // trigger never fired (state missing on this model)
            }
            else if (!inEmote || state.normalizedTime >= 1f)
                break;

            // Want to move? Cut the dance short after a brief beat so it doesn't feel like a trap.
            if (t > 0.25f && (front || back || left || right))
            {
                animator.CrossFadeInFixedTime(returnState, 0.12f, 0);
                PlaySquash(0.12f);
                break;
            }

            if (t >= nextSpark)
            {
                nextSpark = t + 0.3f;
                Vector3 offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * 0.5f;
                VisualFx.Puff(transform.position + Vector3.up * 1.6f + offset, EmoteSparkColors[Random.Range(0, EmoteSparkColors.Length)], 3, 0.15f, 1.8f);
            }

            yield return null;
        }

        isEmoting = false;
        emoteRoutine = null;
    }

    private Coroutine emoteRoutine;

    private void EnsureMovementState()
    {
        if (!IsMovementLocked())
            doMove = true;
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

        if (hasCarryParam)
            animator.SetBool(isCarryingHash, !heldItem.IsEmpty);
    }

    private static readonly int isCarryingHash = Animator.StringToHash("IsCarrying");
    private bool hasCarryParam;

    // Set by GravityPad during a zero-G hop: ignores all input until cleared.
    [System.NonSerialized] public bool inputLocked;

    // Used by ChefVisual to swap in a new chef model at runtime.
    public void SetModel(Animator model)
    {
        animator = model;
        modelBaseScale = model.transform.localScale;
        modelBaseRotation = model.transform.localRotation;
        modelBasePosition = model.transform.localPosition;
        hasCarryParam = System.Array.Exists(model.parameters, p => p.nameHash == isCarryingHash);
        UpdateAnimator();
    }

    public void DropItemAtPosition(Vector3 position)
    {
        if (heldItem.IsEmpty)
            return;

        Vector3 velocity = Vector3.up * dropUpwardVelocity;
        SpawnDroppedItem(heldItem, position, velocity);
        heldItem.Clear();
        UpdateHeldItemHUD();
        RefreshHeldItemVisual();
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
        collider.excludeLayers = LayerMask.GetMask("Walls");

        Rigidbody droppedRigidbody = droppedObject.AddComponent<Rigidbody>();
        droppedRigidbody.mass = 5f;
        droppedRigidbody.useGravity = true;
        droppedRigidbody.linearDamping = 2.5f;
        droppedRigidbody.angularDamping = 4f;
        droppedRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        droppedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        droppedRigidbody.AddForce(velocity, ForceMode.VelocityChange);

        DroppedItem droppedItem = droppedObject.AddComponent<DroppedItem>();
        droppedItem.Initialize(itemData, heldItemVisualizer);
        droppedItem.droppedBy = this;

        Destroy(droppedObject, droppedItemLifetime);
    }

    private System.Collections.IEnumerator CloseAfterDelay(BaseStation station, float delay)
    {
        yield return new WaitForSeconds(delay);
        station.OpenPanel(false);
    }

    private IInteractable GetNearestValidInteractable(float radius, bool requireValid = true)
    {
        int maskValue = hitMask.value == 0 ? ~0 : hitMask.value;
        Collider[] hits = OverlapNearby(radius, maskValue, QueryTriggerInteraction.UseGlobal, out int hitCount);

        IInteractable nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in new System.ArraySegment<Collider>(hits, 0, hitCount))
        {
            IInteractable interactable = GetBestInteractable(hit);

            if (interactable == null)
                continue;

            if (requireValid && (interactable is BaseStation { IsLocked: true } || !interactable.CanInteractWith(this)))
                continue;

            float dist = FacingScore(hit.ClosestPoint(transform.position));

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private IInteractable GetNearestValidDroppedItem(float radius, bool skipOwnFreshDrops = false)
    {
        int maskValue = hitMask.value == 0 ? ~0 : hitMask.value;
        Collider[] hits = OverlapNearby(radius, maskValue, QueryTriggerInteraction.Collide, out int hitCount);

        DroppedItem nearestDropped = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in new System.ArraySegment<Collider>(hits, 0, hitCount))
        {
            DroppedItem dropped = hit.GetComponentInParent<DroppedItem>(); // includes the collider itself; no fake-null `??`

            if (dropped == null && hit.attachedRigidbody != null)
                hit.attachedRigidbody.TryGetComponent(out dropped);

            if (dropped == null)
                continue;

            if (!dropped.CanInteractWith(this))
                continue;

            if (skipOwnFreshDrops && dropped.droppedBy == this)
                continue;

            float dist = FacingScore(hit.ClosestPoint(transform.position));

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
            hits = OverlapNearby(radius, ~0, QueryTriggerInteraction.Collide, out hitCount);

            foreach (Collider hit in new System.ArraySegment<Collider>(hits, 0, hitCount))
            {
                DroppedItem dropped = hit.GetComponentInParent<DroppedItem>(); // includes the collider itself; no fake-null `??`

                if (dropped == null && hit.attachedRigidbody != null)
                    hit.attachedRigidbody.TryGetComponent(out dropped);

                if (dropped == null)
                    continue;

                if (!dropped.CanInteractWith(this))
                    continue;

                if (skipOwnFreshDrops && dropped.droppedBy == this)
                    continue;

                float dist = FacingScore(hit.ClosestPoint(transform.position));

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestDropped = dropped;
                }
            }
        }

        return nearestDropped;
    }

    // Shared, reused query buffer: OverlapSphere allocated a new array on every call, several times a frame.
    private static readonly Collider[] overlapBuffer = new Collider[64];

    private Collider[] OverlapNearby(float radius, int mask, QueryTriggerInteraction triggers, out int count)
    {
        count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, mask, triggers);
        return overlapBuffer;
    }

    private static readonly System.Collections.Generic.List<IInteractable> interactableBuffer = new System.Collections.Generic.List<IInteractable>();

    private IInteractable GetBestInteractable(Collider hit)
    {
        hit.GetComponents(interactableBuffer); // reused list: no per-collider array allocation every frame
        var candidates = interactableBuffer;
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
        UpdateHeldGlow();
    }

    [Tooltip("Soft halo behind held food, so you can tell at a glance who's carrying what")]
    public Color heldGlowColor = new Color(1f, 0.78f, 0.3f, 0.8f);
    public float heldGlowSize = 1.5f;
    private SpriteRenderer heldGlow;
    private static Sprite glowSprite;

    private void UpdateHeldGlow()
    {
        Transform item = heldItemVisualizer != null ? heldItemVisualizer.CurrentVisual : null;
        if (heldGlow == null)
        {
            if (item == null)
                return;

            if (glowSprite == null)
                glowSprite = MakeGlowSprite();
            var go = new GameObject("Held Glow");
            go.transform.SetParent(item.parent, false);
            go.transform.localPosition = heldItemVisualizer.localPosition;
            go.transform.localScale = Vector3.one * heldGlowSize / Mathf.Max(0.0001f, item.parent.lossyScale.x);
            heldGlow = go.AddComponent<SpriteRenderer>();
            heldGlow.sprite = glowSprite;
            heldGlow.color = heldGlowColor;
            heldGlow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<Billboard>();
        }
        heldGlow.gameObject.SetActive(item != null);
    }

    private static Sprite MakeGlowSprite()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = Mathf.Clamp01(new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(255 * (1f - r) * (1f - r)));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n); // 1 unit across
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

    private void OnDisable()
    {
        SetHighlightTarget(null);

        StopSprintAudio();
        StopSprintDust();
    }

    private void OnDestroy()
    {
        StopSprintAudio();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
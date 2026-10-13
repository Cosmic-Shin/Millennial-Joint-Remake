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

    [Header("Movement")]
    public bool doMove = true;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSmooth = 14f;

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
        public KeyCode up, left, down, right, interact, run, drop, emote;
        public ControllerAxisSource padAxis;
        public KeyCode padInteract, padRun, padEmote, padThrow;
    }

    public InputBindings Bindings
    {
        get => new InputBindings
        {
            scheme = controlScheme, joystickNumber = joystickNumber,
            up = MoveUp, left = MoveLeft, down = MoveDown, right = MoveRight,
            interact = PrimaryAction, run = Run, drop = DropItem, emote = EmoteKey,
            padAxis = padMovementAxis,
            padInteract = padInteractKey, padRun = padRunKey, padEmote = padEmoteKey, padThrow = padThrowKey
        };
        set
        {
            controlScheme = value.scheme; joystickNumber = value.joystickNumber;
            MoveUp = value.up; MoveLeft = value.left; MoveDown = value.down; MoveRight = value.right;
            PrimaryAction = value.interact; Run = value.run; DropItem = value.drop; EmoteKey = value.emote;
            padMovementAxis = value.padAxis;
            padInteractKey = value.padInteract; padRunKey = value.padRun; padEmoteKey = value.padEmote; padThrowKey = value.padThrow;
        }
    }

    public void ResetBindingsToDefault()
    {
        bool p1 = playerNumber == 1;
        MoveUp = p1 ? KeyCode.W : KeyCode.UpArrow;
        MoveLeft = p1 ? KeyCode.A : KeyCode.LeftArrow;
        MoveDown = p1 ? KeyCode.S : KeyCode.DownArrow;
        MoveRight = p1 ? KeyCode.D : KeyCode.RightArrow;
        PrimaryAction = p1 ? KeyCode.F : KeyCode.Return;
        Run = p1 ? KeyCode.LeftShift : KeyCode.RightShift;
        DropItem = p1 ? KeyCode.Q : KeyCode.Slash;
        EmoteKey = p1 ? KeyCode.Z : KeyCode.Backspace;

        padMovementAxis = ControllerAxisSource.LSB;
        padInteractKey = KeyCode.Joystick1Button0;
        padRunKey = KeyCode.Joystick1Button1;
        padEmoteKey = KeyCode.Joystick1Button3;
        padThrowKey = KeyCode.Joystick1Button2;
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
            sprintDust.Stop();

        SetupSprintAudio();

        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            modelBaseScale = animator.transform.localScale;

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

        if (!GameplayActive)
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

        if (!IsMovementLocked())
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
            currentIngredientBox.HandleIngredientSelectionInput();

        if (IsPrimaryActionPressed())
            HandleInteraction();

        if (currentDrinkMachine != null)
            currentDrinkMachine.HandleDrinkSelectionInput();

        if (currentIceCreamMachine != null)
            currentIceCreamMachine.HandleIceCreamSelectionInput();

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
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

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

    private void PlaySquash()
    {
        if (animator == null)
            return;

        if (squashRoutine != null)
            StopCoroutine(squashRoutine);

        squashRoutine = StartCoroutine(SquashRoutine(animator.transform));
    }

    private System.Collections.IEnumerator SquashRoutine(Transform model)
    {
        const float duration = 0.3f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            float s = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.2f;
            model.localScale = Vector3.Scale(modelBaseScale, new Vector3(1f + s * 0.5f, 1f - s, 1f + s * 0.5f));
            yield return null;
        }

        model.localScale = modelBaseScale;
        squashRoutine = null;
    }

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
               currentIceCreamMachine != null;
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
            if (GetNearestValidInteractable(castRadius, requireValid: false) != null)
                ShowFloatingText(heldItem.IsEmpty ? "Can't use that right now" : "Can't use " + heldItem.GetDisplayName() + " here", errorTextColor);
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

            interactable.Interact(this);
            UpdateHeldItemHUD();
            RefreshHeldItemVisual();

            if (heldItem.type != heldBefore)
                PlaySquash();

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
                    velocity = transform.forward * strength + Vector3.up * throwUpwardBoost;
                    AudioManager.Instance?.PlayThrowSFX();
                }
                else
                {
                    velocity = Vector3.up * dropUpwardVelocity;
                }

                SpawnDroppedItem(heldItem, dropPosition, velocity);
                heldItem.Clear();
                UpdateHeldItemHUD();
                RefreshHeldItemVisual();
                PlaySquash();
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

        emoteSelectionText.text = emoteSelectionLabels[selectedEmoteIndex];
    }

    private void ShowEmoteSelectionText(bool show)
    {
        if (emoteSelectionObject != null)
            emoteSelectionObject.SetActive(show);
    }

    private void PlayEmote(int emoteHash)
    {
        StopSprintAudio();
        StopSprintDust();

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
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, maskValue);

        IInteractable nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = GetBestInteractable(hit);

            if (interactable == null)
                continue;

            if (requireValid && !interactable.CanInteractWith(this))
                continue;

            float dist = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));

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
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, maskValue, QueryTriggerInteraction.Collide);

        DroppedItem nearestDropped = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            DroppedItem dropped = hit.GetComponent<DroppedItem>() ?? hit.GetComponentInParent<DroppedItem>();

            if (dropped == null && hit.attachedRigidbody != null)
                dropped = hit.attachedRigidbody.GetComponent<DroppedItem>();

            if (dropped == null)
                continue;

            if (!dropped.CanInteractWith(this))
                continue;

            if (skipOwnFreshDrops && dropped.droppedBy == this)
                continue;

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

                if (dropped == null)
                    continue;

                if (!dropped.CanInteractWith(this))
                    continue;

                if (skipOwnFreshDrops && dropped.droppedBy == this)
                    continue;

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
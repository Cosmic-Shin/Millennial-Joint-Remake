using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider solidCollider;
    [SerializeField] private Collider pickupTrigger;

    [Header("Rotation")]
    [SerializeField] private float yRotationSpeed = 90f;

    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.8f;
    [SerializeField] private float bobAmount = 0.3f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    [Header("Random Power-Up")]
    [SerializeField] private PowerUpType powerUpType;

    private bool hasBeenPickedUp;
    private bool hasLanded;

    private Vector3 landingPosition;
    private float bobTimer;

    private Vector3 initialEuler;
    private float currentYRotation;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        RandomizePowerUpType();

        if (solidCollider != null)
            solidCollider.isTrigger = false;

        if (pickupTrigger != null)
            pickupTrigger.isTrigger = true;
    }

    private void Start()
    {
        initialEuler = transform.eulerAngles;
        currentYRotation = initialEuler.y;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            rb.constraints =
                RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void Update()
    {
        currentYRotation += yRotationSpeed * Time.deltaTime;

        transform.rotation = Quaternion.Euler(
            initialEuler.x,
            currentYRotation,
            initialEuler.z
        );

        if (hasLanded)
        {
            bobTimer += Time.deltaTime;
            float bobOffset = Mathf.Sin(bobTimer * bobSpeed) * bobAmount;
            transform.position = landingPosition + Vector3.up * bobOffset;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded)
            return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                LandPowerUp();
                return;
            }
        }
    }

    private void LandPowerUp()
    {
        hasLanded = true;

        landingPosition = transform.position + Vector3.up * floatHeight;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (solidCollider != null)
            solidCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenPickedUp)
            return;

        PlayerControl player = other.GetComponent<PlayerControl>();

        if (player == null)
            player = other.GetComponentInParent<PlayerControl>();

        if (player != null)
            PickUpPowerUp(player);
    }

    private void PickUpPowerUp(PlayerControl player)
    {
        hasBeenPickedUp = true;

        if (pickupTrigger != null)
            pickupTrigger.enabled = false;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        ApplyPowerUpEffect(player);

        Destroy(gameObject);
    }

    private void RandomizePowerUpType()
    {
        PowerUpType[] types = (PowerUpType[])System.Enum.GetValues(typeof(PowerUpType));

        bool versus = OrderManager.Instance != null && OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS;
        if (!versus)
            types = System.Array.FindAll(types, t => !PowerUpManager.IsSabotage(t));

        powerUpType = types[Random.Range(0, types.Length)];
    }

    private void ApplyPowerUpEffect(PlayerControl player)
    {
        switch (powerUpType)
        {
            case PowerUpType.UnlimitedStamina:
                PowerUpManager.Instance?.ActivateUnlimitedStamina(player.playerNumber);
                break;

            case PowerUpType.HalvedCookingTime:
                PowerUpManager.Instance?.ActivateHalvedCookingTime(player.playerNumber);
                break;

            case PowerUpType.RandomFood:
                GiveRandomCompleteFood(player);
                break;
            case PowerUpType.SlowTime:
                PowerUpManager.Instance?.ActivateSlowTime(player.playerNumber);
                break;

            case PowerUpType.FreezeOpponent:
                PowerUpManager.Instance?.FreezeOpponent(player.playerNumber);
                break;

            case PowerUpType.ChangeOpponentOrder:
                PowerUpManager.Instance?.ChangeOpponentOrder(player.playerNumber);
                break;
        }
    }

    private void GiveRandomCompleteFood(PlayerControl player)
    {
        KitchenItemData randomFood = GenerateRandomCompleteFood();

        if (!player.heldItem.IsEmpty)
        {
            player.DropItemAtPosition(player.transform.position + player.transform.forward * 0.8f);
        }

        player.heldItem = randomFood;
        player.RefreshHeldItemDisplay();
        PowerUpManager.Instance?.ShowFreeFoodMessage(player.playerNumber);
    }

    private KitchenItemData GenerateRandomCompleteFood()
    {
        int randomChoice = Random.Range(0, 3);

        KitchenItemData food = new KitchenItemData();

        switch (randomChoice)
        {
            case 0:
                food.Set(ItemType.Plate);
                food.plateHasBun = true;
                food.plateHasPatty = true;
                food.plateHasVeggie = true;
                break;

            case 1:
                food.Set(ItemType.Plate);
                food.plateHasFries = true;
                break;

            case 2:
                food.Set(ItemType.Plate);
                food.plateHasChicken = true;
                break;
        }

        return food;
    }
}
using UnityEngine;
using TMPro;

public enum PowerUpType
{
    UnlimitedStamina,
    HalvedCookingTime,
    RandomFood,
    SlowTime
}

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Power-Up Duration")]
    public float effectDuration = 15f;

    [Header("Power-Up UI")]
    [SerializeField] private TMP_Text powerUpText;

    [Header("Message")]
    [SerializeField] private float messageDuration = 2f;

    private float unlimitedStaminaTimer;
    private float halvedCookingTimer;
    private float slowTimeTimer;
    private float messageTimer;

    public bool HasUnlimitedStamina => unlimitedStaminaTimer > 0f;
    public bool HasHalvedCookingTime => halvedCookingTimer > 0f;
    public bool HasSlowTime => slowTimeTimer > 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HidePowerUpMessages();
    }

    private void Update()
    {
        if (unlimitedStaminaTimer > 0f)
            unlimitedStaminaTimer -= Time.deltaTime;

        if (halvedCookingTimer > 0f)
            halvedCookingTimer -= Time.deltaTime;

        if (slowTimeTimer > 0f)
            slowTimeTimer -= Time.deltaTime;

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0f)
                HidePowerUpMessages();
        }
    }

    public void ActivateUnlimitedStamina()
    {
        unlimitedStaminaTimer = effectDuration;
        ShowPowerUpMessage("Unli Stamina");
    }

    public void ActivateHalvedCookingTime()
    {
        halvedCookingTimer = effectDuration;
        ShowPowerUpMessage("Cook Time Halved");
    }

    public void ActivateSlowTime()
    {
        slowTimeTimer = effectDuration;
        ShowPowerUpMessage("Slow Time");
    }

    public void ShowFreeFoodMessage()
    {
        ShowPowerUpMessage("Free Food");
    }

    public float GetCookingTimeMultiplier()
    {
        return HasHalvedCookingTime ? 0.5f : 1f;
    }

    public float GetGameTimerMultiplier()
    {
        return HasSlowTime ? 0.5f : 1f;
    }

    private void ShowPowerUpMessage(string message)
    {
        if (powerUpText == null)
            return;

        powerUpText.text = message;
        powerUpText.gameObject.SetActive(true);

        messageTimer = messageDuration;
    }

    private void HidePowerUpMessages()
    {
        if (powerUpText != null)
            powerUpText.gameObject.SetActive(false);
    }
}
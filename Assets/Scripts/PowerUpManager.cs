using UnityEngine;
using TMPro;

public enum PowerUpType
{
    UnlimitedStamina,
    HalvedCookingTime,
    RandomFood,
    SlowTime,
    FreezeOpponent,
    ChangeOpponentOrder
}

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Power-Up Duration")]
    public float effectDuration = 15f;

    [Header("Versus Sabotage")]
    public float freezeDuration = 3f;

    [Header("Power-Up UI")]
    [SerializeField] private TMP_Text powerUpText;

    [Header("Message")]
    [SerializeField] private float messageDuration = 2f;

    [Header("Versus Message Colors (who picked it up)")]
    [SerializeField] private Color player1Color = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color player2Color = new Color(0.3f, 0.55f, 1f);

    private Color defaultMessageColor = Color.white;
    private Vector3 messageBaseScale = Vector3.one;

    private readonly float[] unlimitedStaminaTimers = new float[3];
    private readonly float[] halvedCookingTimers = new float[3];
    private readonly float[] slowTimeTimers = new float[3];
    private float messageTimer;

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
        if (powerUpText != null)
        {
            defaultMessageColor = powerUpText.color;
            messageBaseScale = powerUpText.transform.localScale;
        }

        HidePowerUpMessages();
    }

    private Color MessageColor(int playerNumber) =>
        !IsVersus ? defaultMessageColor : Slot(playerNumber) == 1 ? player1Color : player2Color;

    private void Update()
    {
        for (int i = 1; i <= 2; i++)
        {
            unlimitedStaminaTimers[i] -= Time.deltaTime;
            halvedCookingTimers[i] -= Time.deltaTime;
            slowTimeTimers[i] -= Time.deltaTime;
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0f)
                HidePowerUpMessages();
        }
    }

    private static int Slot(int playerNumber) => playerNumber == 2 ? 2 : 1;

    private static bool IsVersus =>
        OrderManager.Instance != null && OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS;

    private void Activate(float[] timers, int playerNumber, string message)
    {
        if (IsVersus)
        {
            timers[Slot(playerNumber)] = effectDuration;
            ShowPowerUpMessage($"P{Slot(playerNumber)}: {message}", playerNumber);
        }
        else
        {
            timers[1] = timers[2] = effectDuration;
            ShowPowerUpMessage(message, playerNumber);
        }
    }

    public void ActivateUnlimitedStamina(int playerNumber) => Activate(unlimitedStaminaTimers, playerNumber, "Unli Stamina");
    public void ActivateHalvedCookingTime(int playerNumber) => Activate(halvedCookingTimers, playerNumber, "Cook Time Halved");
    public void ActivateSlowTime(int playerNumber) => Activate(slowTimeTimers, playerNumber, "Slow Time");

    public static bool IsSabotage(PowerUpType type) =>
        type == PowerUpType.FreezeOpponent || type == PowerUpType.ChangeOpponentOrder;

    private static int Opponent(int playerNumber) => Slot(playerNumber) == 1 ? 2 : 1;

    public void FreezeOpponent(int playerNumber)
    {
        foreach (PlayerControl player in FindObjectsByType<PlayerControl>(FindObjectsSortMode.None))
        {
            if (player.playerNumber == Opponent(playerNumber))
                player.Freeze(freezeDuration);
        }

        ShowPowerUpMessage($"P{Slot(playerNumber)} froze P{Opponent(playerNumber)}!", playerNumber);
        AudioManager.Instance?.PlayFreezeSFX();
    }

    public void ChangeOpponentOrder(int playerNumber)
    {
        if (OrderManager.Instance != null && OrderManager.Instance.ChangeRandomOrder(Opponent(playerNumber)))
            ShowPowerUpMessage($"P{Slot(playerNumber)} changed P{Opponent(playerNumber)}'s order!", playerNumber);
    }

    public void ShowFreeFoodMessage(int playerNumber)
    {
        ShowPowerUpMessage(IsVersus ? $"P{Slot(playerNumber)}: Free Food" : "Free Food", playerNumber);
    }

    public bool HasUnlimitedStamina(int playerNumber) => unlimitedStaminaTimers[Slot(playerNumber)] > 0f;

    public float GetCookingTimeMultiplier(PlayerControl user)
    {
        return user != null && halvedCookingTimers[Slot(user.playerNumber)] > 0f ? 0.5f : 1f;
    }

    public float GetSlowTimeMultiplier(int playerNumber)
    {
        return slowTimeTimers[Slot(playerNumber)] > 0f ? 0.5f : 1f;
    }

    private void ShowPowerUpMessage(string message, int playerNumber)
    {
        if (powerUpText == null)
            return;

        powerUpText.text = message;
        powerUpText.color = MessageColor(playerNumber);
        powerUpText.gameObject.SetActive(true);
        StartCoroutine(VisualFx.Pop(powerUpText.transform, 1.4f, 0.3f, messageBaseScale));

        messageTimer = messageDuration;
    }

    private void HidePowerUpMessages()
    {
        if (powerUpText != null)
            powerUpText.gameObject.SetActive(false);
    }
}

using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public enum GameMode { TIME, SPEED }

    [Header("Order Prices")]
    public float burgerPrice       = 8f;
    public float sandwichPrice     = 10f;
    public float friedChickenPrice = 9f;
    public float friesPrice        = 3.5f;
    public float sodaPrice         = 2.5f;
    public float iceTeaPrice       = 2.5f;
    public float orangeJuicePrice  = 3f;
    public float coffeePrice       = 3.5f;

    [Header("Economy")]
    public float money = 0f;

    [Header("Game Mode")]
    public float gameTimer  = 300f;
    public float moneyQuota = 100f;
    public float speedModeQuota = 50f;  // Quota for speed mode

    private float currentTime;
    private bool quotaReached = false;
    private Order currentOrder;
    private GameMode currentMode = GameMode.TIME;

    public enum GameState { Waiting, Playing, Won, Lost }
    public GameState state = GameState.Waiting;

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
        // Game starts in waiting state (mode selection shown)
        state = GameState.Waiting;
    }

    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
        money = 0f;
        quotaReached = false;
        state = GameState.Playing;

        if (mode == GameMode.TIME)
        {
            currentTime = 180f;  // 3 minutes
            moneyQuota = 100f;
        }
        else  // SPEED mode
        {
            currentTime = 0f;  // Timer counts up
            moneyQuota = speedModeQuota;
        }

        GenerateNewOrder();
        OrderUIManager.Instance?.UpdateGameUI();
        Debug.Log($"Game Mode: {(mode == GameMode.TIME ? "TIME (3 mins)" : "SPEED (Race Quota)")} | Quota: ${moneyQuota}");
    }

    private void Update()
    {
        if (state != GameState.Playing) return;

        if (currentMode == GameMode.TIME)
        {
            currentTime -= Time.deltaTime;
        }
        else  // SPEED mode - timer counts up
        {
            currentTime += Time.deltaTime;
        }

        if (money >= moneyQuota && !quotaReached)
        {
            quotaReached = true;

            if (currentMode == GameMode.TIME)
            {
                Debug.Log("Quota reached! Continue until timer ends.");
            }
            else  // SPEED mode
            {
                state = GameState.Won;
                Debug.Log($"Speed Mode Complete! Time: {currentTime:F1}s");
                OrderUIManager.Instance?.UpdateGameUI();
                HandleGameEnd();
            }
        }

        if (currentMode == GameMode.TIME && currentTime <= 0)
        {
            state = quotaReached ? GameState.Won : GameState.Lost;
            Debug.Log(state == GameState.Won ? "Game Won! (Time expired with quota)" : "Game Over! (Time expired, no quota)");
            OrderUIManager.Instance?.UpdateGameUI();
            HandleGameEnd();
        }
        else
        {
            OrderUIManager.Instance?.UpdateGameUI();
        }
    }

    public void GenerateNewOrder()
    {
        if (state != GameState.Playing) return;

        currentOrder = new Order();
        currentOrder.GenerateRandomOrder();
        Debug.Log("New order generated:\n" + currentOrder.GetDisplayText());
        OrderUIManager.Instance?.UpdateDisplay(currentOrder);
    }

    public bool TryServeItem(KitchenItemData item)
    {
        if (currentOrder == null) return false;

        OrderItemType? servedType = currentOrder.TryServeItem(item);
        if (servedType == null) return false;

        money += GetPriceForType(servedType.Value);

        if (currentOrder.IsComplete())
        {
            Debug.Log("Order complete! Generating new order.");
            GenerateNewOrder();
        }
        else
        {
            OrderUIManager.Instance?.UpdateDisplay(currentOrder);
        }

        return true;
    }

    private void HandleGameEnd()
    {
        // Show mode selector after a brief delay
        Invoke(nameof(ShowModeSelector), 2f);
    }

    private void ShowModeSelector()
    {
        GameModeSelector.Instance?.ShowModeSelector();
    }

    private float GetPriceForType(OrderItemType type)
    {
        return type switch
        {
            OrderItemType.Burger       => burgerPrice,
            OrderItemType.Sandwich     => sandwichPrice,
            OrderItemType.FriedChicken => friedChickenPrice,
            OrderItemType.Fries        => friesPrice,
            OrderItemType.Soda         => sodaPrice,
            OrderItemType.IceTea       => iceTeaPrice,
            OrderItemType.OrangeJuice  => orangeJuicePrice,
            OrderItemType.Coffee       => coffeePrice,
            _                          => 0f,
        };
    }

    public float GetCurrentTime()  => currentTime;
    public Order GetCurrentOrder() => currentOrder;
    public GameMode GetCurrentMode() => currentMode;
}
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public enum GameMode { TIME, SPEED }

    [Header("Order Prices")]
    public float burgerPrice = 8f;
    public float sandwichPrice = 10f;
    public float friedChickenPrice = 9f;
    public float friesPrice = 3.5f;
    public float sodaPrice = 2.5f;
    public float iceTeaPrice = 2.5f;
    public float orangeJuicePrice = 3f;
    public float coffeePrice = 3.5f;
    public float chiliDogPrice = 9f;

    [Header("Economy")]
    public float money = 0f;

    [Header("Game Mode")]
    public float timeModeDuration = 180f;
    public float timeModeQuota = 100f;
    public float speedModeQuota = 50f;
    public float returnToModeSelectDelay = 2f;

    private float currentTime;
    private Order currentOrder;
    private GameMode currentMode = GameMode.TIME;
    private bool gameEnding;

    public enum GameState { Waiting, Playing, Won, Lost }
    public GameState state = GameState.Waiting;

    public float moneyQuota = 100f;

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
        ResetToWaiting();
    }

    public void SetGameMode(GameMode mode)
    {
        CancelInvoke();

        currentMode = mode;
        money = 0f;
        gameEnding = false;
        state = GameState.Playing;

        OrderUIManager.Instance?.HideStatus();

        if (mode == GameMode.TIME)
        {
            currentTime = timeModeDuration;
            moneyQuota = timeModeQuota;
        }
        else
        {
            currentTime = 0f;
            moneyQuota = speedModeQuota;
        }

        GenerateNewOrder();
        OrderUIManager.Instance?.UpdateGameUI();

        Debug.Log($"Game Mode: {mode} | Goal: ${moneyQuota}");
    }

    private void Update()
    {
        if (state != GameState.Playing || gameEnding)
            return;

        if (currentMode == GameMode.TIME)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                EndGame(money >= moneyQuota);
                return;
            }
        }
        else
        {
            currentTime += Time.deltaTime;

            if (money >= moneyQuota)
            {
                EndGame(true);
                return;
            }
        }

        OrderUIManager.Instance?.UpdateGameUI();
    }

    public void GenerateNewOrder()
    {
        if (state != GameState.Playing)
            return;

        currentOrder = new Order();
        currentOrder.GenerateRandomOrder();

        Debug.Log("New order generated:\n" + currentOrder.GetDisplayText());
        OrderUIManager.Instance?.UpdateDisplay(currentOrder);
    }

    public bool TryServeItem(KitchenItemData item)
    {
        if (state != GameState.Playing || currentOrder == null)
            return false;

        OrderItemType? servedType = currentOrder.TryServeItem(item);

        if (servedType == null)
            return false;

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

        OrderUIManager.Instance?.UpdateGameUI();

        if (currentMode == GameMode.SPEED && money >= moneyQuota)
            EndGame(true);

        return true;
    }

    private void EndGame(bool won)
    {
        gameEnding = true;
        state = won ? GameState.Won : GameState.Lost;

        OrderUIManager.Instance?.UpdateGameUI();
        OrderUIManager.Instance?.ShowStatus(won);

        Debug.Log(won ? "You Win" : "You Lose");

        Invoke(nameof(ReturnToModeSelect), returnToModeSelectDelay);
    }

    private void ReturnToModeSelect()
    {
        ResetToWaiting();
        GameModeSelector.Instance?.ShowModeSelector();
    }

    private void ResetToWaiting()
    {
        state = GameState.Waiting;
        gameEnding = false;
        currentOrder = null;
        currentTime = 0f;
        money = 0f;

        OrderUIManager.Instance?.HideStatus();
        OrderUIManager.Instance?.ClearOrderImages();
        OrderUIManager.Instance?.UpdateGameUI();
    }

    private float GetPriceForType(OrderItemType type)
    {
        return type switch
        {
            OrderItemType.Burger => burgerPrice,
            OrderItemType.Sandwich => sandwichPrice,
            OrderItemType.FriedChicken => friedChickenPrice,
            OrderItemType.Fries => friesPrice,
            OrderItemType.Soda => sodaPrice,
            OrderItemType.IceTea => iceTeaPrice,
            OrderItemType.OrangeJuice => orangeJuicePrice,
            OrderItemType.Coffee => coffeePrice,
            OrderItemType.ChiliDog => chiliDogPrice,
            _ => 0f
        };
    }

    public float GetCurrentTime() => currentTime;
    public Order GetCurrentOrder() => currentOrder;
    public GameMode GetCurrentMode() => currentMode;
}
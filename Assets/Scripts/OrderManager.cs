using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

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

    private float currentTime;
    private bool quotaReached = false;
    private Order currentOrder;

    public enum GameState { Playing, Won, Lost }
    public GameState state = GameState.Playing;

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
        money        = Mathf.Max(0f, money);
        currentTime  = Mathf.Max(gameTimer, 0.1f);
        quotaReached = false;
        state        = GameState.Playing;
        GenerateNewOrder();
        OrderUIManager.Instance?.UpdateGameUI();
    }

    private void Update()
    {
        if (state != GameState.Playing) return;

        currentTime -= Time.deltaTime;

        if (money >= moneyQuota && !quotaReached)
        {
            quotaReached = true;
            Debug.Log("Quota reached! Continue until timer ends.");
        }

        if (currentTime <= 0)
        {
            state = quotaReached ? GameState.Won : GameState.Lost;
            Debug.Log(state == GameState.Won ? "Game Won!" : "Game Over!");
            OrderUIManager.Instance?.UpdateGameUI();
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
}
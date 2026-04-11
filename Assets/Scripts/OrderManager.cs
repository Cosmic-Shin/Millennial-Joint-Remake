using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    [Header("Order Prices")]
    public float burgerPrice = 8f;
    public float baconBurgerPrice = 11f;
    public float friedChickenPrice = 9f;
    public float sodaPrice = 2.5f;
    public float bobaPrice = 4f;
    public float friesPrice = 3.5f;

    [Header("Economy")]
    public float money = 0f;

    private Order currentOrder;

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
        money = Mathf.Max(0f, money);
        GenerateNewOrder();
    }

    public void GenerateNewOrder()
    {
        currentOrder = new Order();
        currentOrder.GenerateRandomOrder();
        Debug.Log("New order generated:\n" + currentOrder.GetDisplayText());
        OrderUIManager.Instance?.UpdateDisplay(currentOrder);
    }

    public bool TryServeItem(KitchenItemData item)
    {
        if (currentOrder == null) return false;

        OrderItemType? servedType = currentOrder.TryServeItem(item);
        if (servedType.HasValue)
        {
            float earned = GetPriceForType(servedType.Value);
            money += earned;
            Debug.Log("Item served! Earned $" + earned + ". Progress: " + currentOrder.GetServedCount() + "/2\n" + currentOrder.GetDisplayText());
            OrderUIManager.Instance?.UpdateDisplay(currentOrder);

            if (currentOrder.IsComplete())
            {
                Debug.Log("Order complete! Generating new order...");
                GenerateNewOrder();
            }

            return true;
        }

        Debug.Log("Item does not match current order");
        return false;
    }

    private float GetPriceForType(OrderItemType type)
    {
        return type switch
        {
            OrderItemType.Burger => burgerPrice,
            OrderItemType.BaconBurger => baconBurgerPrice,
            OrderItemType.FriedChicken => friedChickenPrice,
            OrderItemType.Soda => sodaPrice,
            OrderItemType.Boba => bobaPrice,
            OrderItemType.Fries => friesPrice,
            _ => 0f,
        };
    }

    public Order GetCurrentOrder()
    {
        return currentOrder;
    }
}

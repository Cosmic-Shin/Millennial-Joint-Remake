using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderUIManager : MonoBehaviour
{
    public static OrderUIManager Instance { get; private set; }

    [Header("Order Images")]
    public Image order1Image;
    public Image order2Image;

    [Header("Served Overlay")]
    public GameObject order1ServedOverlay;
    public GameObject order2ServedOverlay;

    [Header("Order Sprites")]
    public Sprite burgerSprite;
    public Sprite sandwichSprite;
    public Sprite friedChickenSprite;
    public Sprite friesSprite;
    public Sprite sodaSprite;
    public Sprite iceTeaSprite;
    public Sprite orangeJuiceSprite;
    public Sprite coffeeSprite;
    public Sprite chiliDogSprite;

    [Header("TMP UI")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

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
        HideStatus();
        ClearOrderImages();
        UpdateMoneyDisplay();
    }

    public void UpdateDisplay(Order order)
    {
        if (order == null)
        {
            ClearOrderImages();
            return;
        }

        UpdateOrderImage(order1Image, order.GetItem(0));
        UpdateOrderImage(order2Image, order.GetItem(1));

        if (order1ServedOverlay != null)
            order1ServedOverlay.SetActive(order.IsServed(0));

        if (order2ServedOverlay != null)
            order2ServedOverlay.SetActive(order.IsServed(1));

        UpdateMoneyDisplay();
    }

    private void UpdateOrderImage(Image image, OrderItem item)
    {
        if (image == null)
            return;

        if (item == null)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = GetSpriteForOrder(item.type);
        image.enabled = image.sprite != null;
    }

    private Sprite GetSpriteForOrder(OrderItemType type)
    {
        return type switch
        {
            OrderItemType.Burger => burgerSprite,
            OrderItemType.Sandwich => sandwichSprite,
            OrderItemType.FriedChicken => friedChickenSprite,
            OrderItemType.Fries => friesSprite,
            OrderItemType.Soda => sodaSprite,
            OrderItemType.IceTea => iceTeaSprite,
            OrderItemType.OrangeJuice => orangeJuiceSprite,
            OrderItemType.Coffee => coffeeSprite,
            OrderItemType.ChiliDog => chiliDogSprite,
            _ => null
        };
    }

    public void UpdateGameUI()
    {
        if (OrderManager.Instance == null)
            return;

        if (timerText != null)
        {
            float time = OrderManager.Instance.GetCurrentTime();

            if (OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.TIME)
                timerText.text = Mathf.Ceil(Mathf.Max(0, time)) + "s";
            else
                timerText.text = Mathf.Ceil(time) + "s";
        }

        if (goalText != null)
            goalText.text = "Goal: $" + OrderManager.Instance.moneyQuota.ToString("0.00");

        UpdateMoneyDisplay();
    }

    public void UpdateMoneyDisplay()
    {
        if (moneyText == null)
            return;

        if (OrderManager.Instance != null)
            moneyText.text = "$" + OrderManager.Instance.money.ToString("0.00");
        else
            moneyText.text = "$0.00";
    }

    public void ShowStatus(bool won)
    {
        if (statusText == null)
            return;

        statusText.gameObject.SetActive(true);
        statusText.text = won ? "You Win" : "You Lose";
    }

    public void HideStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }
    }

    public void ClearOrderImages()
    {
        if (order1Image != null)
        {
            order1Image.sprite = null;
            order1Image.enabled = false;
        }

        if (order2Image != null)
        {
            order2Image.sprite = null;
            order2Image.enabled = false;
        }

        if (order1ServedOverlay != null)
            order1ServedOverlay.SetActive(false);

        if (order2ServedOverlay != null)
            order2ServedOverlay.SetActive(false);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderUIManager : MonoBehaviour
{
    public static OrderUIManager Instance { get; private set; }

    [Header("Order Images")]
    public Image order1Image;
    public Image order2Image;

    [Header("Served Overlay / Indicator Images")]
    public GameObject order1ServedOverlay;
    public GameObject order2ServedOverlay;

    [Header("Served Indicator Fade")]
    public float servedIndicatorStayTime = 0.5f;
    public float servedIndicatorFadeTime = 0.5f;

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

    private bool isChangingOrders;
    private Order pendingOrder;
    private Coroutine changeOrderCoroutine;

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
        if (isChangingOrders)
        {
            pendingOrder = order;
            return;
        }

        if (order == null)
        {
            ClearOrderImages();
            return;
        }

        bool order1Served = order.IsServed(0);
        bool order2Served = order.IsServed(1);
        bool bothServed = order1Served && order2Served;

        UpdateOrderImage(order1Image, order.GetItem(0));
        UpdateOrderImage(order2Image, order.GetItem(1));

        SetOverlay(order1ServedOverlay, order1Served, 1f);
        SetOverlay(order2ServedOverlay, order2Served, 1f);

        UpdateMoneyDisplay();

        if (bothServed)
        {
            if (changeOrderCoroutine != null)
                StopCoroutine(changeOrderCoroutine);

            changeOrderCoroutine = StartCoroutine(ShowServedThenChangeOrder());
        }
    }

    private IEnumerator ShowServedThenChangeOrder()
    {
        isChangingOrders = true;

        SetOverlay(order1ServedOverlay, true, 1f);
        SetOverlay(order2ServedOverlay, true, 1f);

        yield return new WaitForSeconds(servedIndicatorStayTime);

        float timer = 0f;

        CanvasGroup overlay1Group = GetCanvasGroup(order1ServedOverlay);
        CanvasGroup overlay2Group = GetCanvasGroup(order2ServedOverlay);

        while (timer < servedIndicatorFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / servedIndicatorFadeTime);

            if (overlay1Group != null)
                overlay1Group.alpha = alpha;

            if (overlay2Group != null)
                overlay2Group.alpha = alpha;

            yield return null;
        }

        SetOverlay(order1ServedOverlay, false, 1f);
        SetOverlay(order2ServedOverlay, false, 1f);

        isChangingOrders = false;

        if (pendingOrder != null)
        {
            Order nextOrder = pendingOrder;
            pendingOrder = null;
            UpdateDisplay(nextOrder);
        }
        else
        {
            ClearOrderImages();
        }
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

    private void SetOverlay(GameObject overlay, bool active, float alpha)
    {
        if (overlay == null)
            return;

        overlay.SetActive(active);

        CanvasGroup group = GetCanvasGroup(overlay);

        if (group != null)
            group.alpha = alpha;
    }

    private CanvasGroup GetCanvasGroup(GameObject obj)
    {
        if (obj == null)
            return null;

        CanvasGroup group = obj.GetComponent<CanvasGroup>();

        if (group == null)
            group = obj.AddComponent<CanvasGroup>();

        return group;
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

        SetOverlay(order1ServedOverlay, false, 1f);
        SetOverlay(order2ServedOverlay, false, 1f);
    }
}
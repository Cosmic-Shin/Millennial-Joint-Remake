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
    public Image order3Image;

    [Header("Served Overlay / Indicator Images")]
    public GameObject order1ServedOverlay;
    public GameObject order2ServedOverlay;
    public GameObject order3ServedOverlay;

    [Header("Order Timer TMPs")]
    public TextMeshProUGUI order1TimerText;
    public TextMeshProUGUI order2TimerText;
    public TextMeshProUGUI order3TimerText;

    [Header("Versus Mode Timer TMPs")]
    public TextMeshProUGUI player1Order1TimerText;
    public TextMeshProUGUI player1Order2TimerText;
    public TextMeshProUGUI player1Order3TimerText;
    public TextMeshProUGUI player2Order1TimerText;
    public TextMeshProUGUI player2Order2TimerText;
    public TextMeshProUGUI player2Order3TimerText;

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
    public Sprite strawberryIceCreamSprite;
    public Sprite bubblegumIceCreamSprite;
    public Sprite mangoIceCreamSprite;

    [Header("TMP UI")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    [Header("Normal Gameplay UI Root")]
    public GameObject normalGameplayPanel;

    private bool isChangingOrders;
    private bool isChangingPlayer1Orders;
    private bool isChangingPlayer2Orders;
    private Coroutine shiftOrdersCoroutine;
    private Coroutine versusShiftPlayer1Coroutine;
    private Coroutine versusShiftPlayer2Coroutine;

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

    private void Update()
    {
        if (OrderManager.Instance == null || OrderManager.Instance.state != OrderManager.GameState.Playing)
            return;

        if (OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS)
            UpdateVersusDisplay(OrderManager.Instance.GetPlayer1OrderQueue(), OrderManager.Instance.GetPlayer2OrderQueue());
        else
            UpdateNormalDisplay(OrderManager.Instance.GetOrderQueue());
    }

    public void UpdateNormalDisplay(System.Collections.Generic.List<QueuedOrder> queue)
    {
        if (isChangingOrders)
            return;

        Image[] images = new[] { order1Image, order2Image, order3Image };
        TextMeshProUGUI[] timerTexts = new[] { order1TimerText, order2TimerText, order3TimerText };

        for (int i = 0; i < 3; i++)
        {
            if (i < queue.Count && queue[i] != null)
            {
                // Display the order
                if (images[i] != null)
                {
                    images[i].sprite = GetSpriteForOrder(queue[i].item.type);
                    images[i].enabled = true;
                }

                // Update timer
                if (timerTexts[i] != null)
                {
                    int timerDisplayValue = Mathf.CeilToInt(Mathf.Max(0, queue[i].timer));
                    timerTexts[i].text = timerDisplayValue.ToString();
                    timerTexts[i].color = queue[i].timer <= 10f ? Color.red : Color.white;
                }
            }
            else
            {
                // Clear slot
                if (images[i] != null)
                {
                    images[i].sprite = null;
                    images[i].enabled = false;
                }
                if (timerTexts[i] != null)
                    timerTexts[i].text = "";
            }
        }

        UpdateMoneyDisplay();
    }

   public void UpdateVersusDisplay(System.Collections.Generic.List<QueuedOrder> player1Queue, System.Collections.Generic.List<QueuedOrder> player2Queue)
{
    TextMeshProUGUI[] p1TimerTexts = new[] { player1Order1TimerText, player1Order2TimerText, player1Order3TimerText };
    TextMeshProUGUI[] p2TimerTexts = new[] { player2Order1TimerText, player2Order2TimerText, player2Order3TimerText };
    Image[] images = new[] { order1Image, order2Image, order3Image };

    if (!isChangingPlayer1Orders)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i < player1Queue.Count && player1Queue[i] != null)
            {
                if (images[i] != null)
                {
                    images[i].sprite = GetSpriteForOrder(player1Queue[i].item.type);
                    images[i].enabled = true;
                }
                if (p1TimerTexts[i] != null)
                {
                    int timerDisplayValue = Mathf.CeilToInt(Mathf.Max(0, player1Queue[i].timer));
                    p1TimerTexts[i].text = timerDisplayValue.ToString();
                    p1TimerTexts[i].color = player1Queue[i].timer <= 10f ? Color.red : Color.white;
                }
            }
            else
            {
                if (images[i] != null) { images[i].sprite = null; images[i].enabled = false; }
                if (p1TimerTexts[i] != null) p1TimerTexts[i].text = "";
            }
        }
    }

    if (!isChangingPlayer2Orders)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i < player2Queue.Count && player2Queue[i] != null)
            {
                if (p2TimerTexts[i] != null)
                {
                    int timerDisplayValue = Mathf.CeilToInt(Mathf.Max(0, player2Queue[i].timer));
                    p2TimerTexts[i].text = timerDisplayValue.ToString();
                    p2TimerTexts[i].color = player2Queue[i].timer <= 10f ? Color.red : Color.white;
                }
            }
            else
            {
                if (p2TimerTexts[i] != null) p2TimerTexts[i].text = "";
            }
        }
    }
}

    public void OnOrderServed(int index, System.Action onComplete)
    {
        if (shiftOrdersCoroutine != null)
            StopCoroutine(shiftOrdersCoroutine);

        shiftOrdersCoroutine = StartCoroutine(ShiftOrdersCoroutine(index, onComplete));
    }

    private IEnumerator ShiftOrdersCoroutine(int index, System.Action onComplete)
{
    isChangingOrders = true;

    GameObject[] overlays = new[] { order1ServedOverlay, order2ServedOverlay, order3ServedOverlay };

    if (index >= 0 && index < overlays.Length && overlays[index] != null)
    {
        SetOverlay(overlays[index], true, 1f);

        yield return new WaitForSeconds(servedIndicatorStayTime);

        float timer = 0f;
        CanvasGroup overlayGroup = GetCanvasGroup(overlays[index]);

        while (timer < servedIndicatorFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / servedIndicatorFadeTime);
            if (overlayGroup != null)
                overlayGroup.alpha = alpha;
            yield return null;
        }

        SetOverlay(overlays[index], false, 1f);
    }

    onComplete?.Invoke();

    isChangingOrders = false;
}

    public void OnVersusOrderServed(int playerNumber, int index, System.Action onComplete)
{
    if (playerNumber == 1)
    {
        if (versusShiftPlayer1Coroutine != null)
            StopCoroutine(versusShiftPlayer1Coroutine);
        versusShiftPlayer1Coroutine = StartCoroutine(ShiftVersusOrdersCoroutine(playerNumber, index, onComplete));
    }
    else
    {
        if (versusShiftPlayer2Coroutine != null)
            StopCoroutine(versusShiftPlayer2Coroutine);
        versusShiftPlayer2Coroutine = StartCoroutine(ShiftVersusOrdersCoroutine(playerNumber, index, onComplete));
    }
}

private IEnumerator ShiftVersusOrdersCoroutine(int playerNumber, int index, System.Action onComplete)
{
    if (playerNumber == 1) isChangingPlayer1Orders = true;
    else isChangingPlayer2Orders = true;

    GameObject[] overlays = new[] { order1ServedOverlay, order2ServedOverlay, order3ServedOverlay };

    if (index >= 0 && index < overlays.Length && overlays[index] != null)
    {
        SetOverlay(overlays[index], true, 1f);

        yield return new WaitForSeconds(servedIndicatorStayTime);

        float timer = 0f;
        CanvasGroup overlayGroup = GetCanvasGroup(overlays[index]);

        while (timer < servedIndicatorFadeTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / servedIndicatorFadeTime);
            if (overlayGroup != null)
                overlayGroup.alpha = alpha;
            yield return null;
        }

        SetOverlay(overlays[index], false, 1f);
    }

    onComplete?.Invoke();

    if (playerNumber == 1) isChangingPlayer1Orders = false;
    else isChangingPlayer2Orders = false;
}

    public void ShowNormalGameplayUI()
    {
        if (normalGameplayPanel != null)
            normalGameplayPanel.SetActive(true);
    }

    public void HideNormalGameplayUI()
    {
        if (normalGameplayPanel != null)
            normalGameplayPanel.SetActive(false);
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
            OrderItemType.StrawberryIceCream => strawberryIceCreamSprite,
            OrderItemType.BubblegumIceCream => bubblegumIceCreamSprite,
            OrderItemType.MangoIceCream => mangoIceCreamSprite,
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

        if (order3Image != null)
        {
            order3Image.sprite = null;
            order3Image.enabled = false;
        }

        SetOverlay(order1ServedOverlay, false, 1f);
        SetOverlay(order2ServedOverlay, false, 1f);
        SetOverlay(order3ServedOverlay, false, 1f);

        if (order1TimerText != null) order1TimerText.text = "";
        if (order2TimerText != null) order2TimerText.text = "";
        if (order3TimerText != null) order3TimerText.text = "";
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class VersusUIManager : MonoBehaviour
{
    public static VersusUIManager Instance { get; private set; }

    [Header("Versus Panels")]
    public GameObject versusPanel;
    public GameObject versusEndPanel;

    [Header("Versus TMP")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI player1MoneyText;
    public TextMeshProUGUI player2MoneyText;

    [Header("End Panel TMP")]
    public TextMeshProUGUI endResultText;

    [Header("End Panel Buttons")]
    public Button retryButton;
    public Button quitButton;

    [Header("Player 1 Order Images")]
    public Image p1Order1Image;
    public Image p1Order2Image;
    public Image p1Order3Image;

    [Header("Player 1 Served Indicators")]
    public GameObject p1Order1ServedOverlay;
    public GameObject p1Order2ServedOverlay;
    public GameObject p1Order3ServedOverlay;

    [Header("Player 2 Order Images")]
    public Image p2Order1Image;
    public Image p2Order2Image;
    public Image p2Order3Image;

    [Header("Player 2 Served Indicators")]
    public GameObject p2Order1ServedOverlay;
    public GameObject p2Order2ServedOverlay;
    public GameObject p2Order3ServedOverlay;

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
    
    [Header("Ice Cream Order Sprites")]
    public Sprite strawberryIceCreamSprite;
    public Sprite bubblegumIceCreamSprite;
    public Sprite mangoIceCreamSprite;

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
        HideVersusUI();

        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
    }

    public void ShowVersusUI()
    {
        if (versusPanel != null)
            versusPanel.SetActive(true);

        if (versusEndPanel != null)
            versusEndPanel.SetActive(false);

        UpdateAllUI();
    }

    public void HideVersusUI()
    {
        if (versusPanel != null)
            versusPanel.SetActive(false);

        if (versusEndPanel != null)
            versusEndPanel.SetActive(false);
    }

    public void UpdateAllUI()
    {
        if (OrderManager.Instance == null)
            return;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(OrderManager.Instance.GetCurrentTime()) + "s";

        if (player1MoneyText != null)
            player1MoneyText.text = "P1: $" + OrderManager.Instance.player1Money.ToString("0.00");

        if (player2MoneyText != null)
            player2MoneyText.text = "P2: $" + OrderManager.Instance.player2Money.ToString("0.00");

        UpdatePlayer1Orders(OrderManager.Instance.GetPlayer1VersusOrder());
        UpdatePlayer2Orders(OrderManager.Instance.GetPlayer2VersusOrder());
    }

    private void UpdatePlayer1Orders(Order order)
    {
        UpdateOrderImage(p1Order1Image, order?.GetItem(0));
        UpdateOrderImage(p1Order2Image, order?.GetItem(1));
        UpdateOrderImage(p1Order3Image, order?.GetItem(2));

        SetOverlay(p1Order1ServedOverlay, order != null && order.IsServed(0));
        SetOverlay(p1Order2ServedOverlay, order != null && order.IsServed(1));
        SetOverlay(p1Order3ServedOverlay, order != null && order.IsServed(2));
    }

    private void UpdatePlayer2Orders(Order order)
    {
        UpdateOrderImage(p2Order1Image, order?.GetItem(0));
        UpdateOrderImage(p2Order2Image, order?.GetItem(1));
        UpdateOrderImage(p2Order3Image, order?.GetItem(2));

        SetOverlay(p2Order1ServedOverlay, order != null && order.IsServed(0));
        SetOverlay(p2Order2ServedOverlay, order != null && order.IsServed(1));
        SetOverlay(p2Order3ServedOverlay, order != null && order.IsServed(2));
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

    private void SetOverlay(GameObject overlay, bool active)
    {
        if (overlay != null)
            overlay.SetActive(active);
    }

    public void ShowVersusEndPanel(string result)
    {
        Time.timeScale = 0f;

        if (versusEndPanel != null)
            versusEndPanel.SetActive(true);

        if (endResultText != null)
        {
            endResultText.text =
                result +
                "\nP1 Money: $" + OrderManager.Instance.player1Money.ToString("0.00") +
                "\nP2 Money: $" + OrderManager.Instance.player2Money.ToString("0.00");
        }
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Quit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
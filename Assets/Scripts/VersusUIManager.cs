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
    public TextMeshProUGUI p1ResultText;
    public TextMeshProUGUI p2ResultText;

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
            quitButton.onClick.AddListener(Menu);
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
        {
            float warning = OrderUIManager.Instance != null ? OrderUIManager.Instance.timerWarningSeconds : 15f;
            OrderUIManager.SetRoundTimer(timerText, OrderManager.Instance.GetCurrentTime(), true, warning);
        }

        if (player1MoneyText != null)
            player1MoneyText.text = OrderUIManager.Money(OrderManager.Instance.player1Money);

        if (player2MoneyText != null)
            player2MoneyText.text = OrderUIManager.Money(OrderManager.Instance.player2Money);

        UpdatePlayer1Orders(OrderManager.Instance.GetPlayer1OrderQueue());
        UpdatePlayer2Orders(OrderManager.Instance.GetPlayer2OrderQueue());
    }

    private void UpdatePlayer1Orders(System.Collections.Generic.List<QueuedOrder> queue)
    {
        for (int i = 0; i < 3; i++)
        {
            Image[] images = new[] { p1Order1Image, p1Order2Image, p1Order3Image };
            GameObject[] overlays = new[] { p1Order1ServedOverlay, p1Order2ServedOverlay, p1Order3ServedOverlay };

            if (i < queue.Count && queue[i] != null)
            {
                UpdateOrderImage(images[i], queue[i].item);
                SetOverlay(overlays[i], queue[i].IsServed());
            }
            else
            {
                UpdateOrderImage(images[i], null);
                SetOverlay(overlays[i], false);
            }
        }
    }

    private void UpdatePlayer2Orders(System.Collections.Generic.List<QueuedOrder> queue)
    {
        for (int i = 0; i < 3; i++)
        {
            Image[] images = new[] { p2Order1Image, p2Order2Image, p2Order3Image };
            GameObject[] overlays = new[] { p2Order1ServedOverlay, p2Order2ServedOverlay, p2Order3ServedOverlay };

            if (i < queue.Count && queue[i] != null)
            {
                UpdateOrderImage(images[i], queue[i].item);
                SetOverlay(overlays[i], queue[i].IsServed());
            }
            else
            {
                UpdateOrderImage(images[i], null);
                SetOverlay(overlays[i], false);
            }
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

        StartCoroutine(VersusEndRoutine(result));
    }

    [Header("End Screen Confetti (winner's color)")]
    public Color player1Color = new Color(1f, 0.25f, 0.25f);
    public Color player2Color = new Color(0.3f, 0.55f, 1f);

    private System.Collections.IEnumerator VersusEndRoutine(string result)
    {
        float p1 = OrderManager.Instance.player1Money;
        float p2 = OrderManager.Instance.player2Money;

        if (endResultText != null)
        {
            endResultText.text = result +
                $"\n<size=30%>All-time wins - P1: {PlayerPrefs.GetInt("VS_P1Wins", 0)}  P2: {PlayerPrefs.GetInt("VS_P2Wins", 0)}  Draws: {PlayerPrefs.GetInt("VS_Draws", 0)}</size>";
            StartCoroutine(VisualFx.Pop(endResultText.transform, 1.15f, 0.3f));
        }

        yield return VisualFx.CountUp(1f, 1.2f, k =>
        {
            SetPlayerResult(p1ResultText, 1, k < 1f ? Mathf.Round(p1 * k) : p1, p1 > p2);
            SetPlayerResult(p2ResultText, 2, k < 1f ? Mathf.Round(p2 * k) : p2, p2 > p1);
        });

        if (versusEndPanel != null)
        {
            Color winnerColor = p1 > p2 ? player1Color : p2 > p1 ? player2Color : Color.yellow;
            StartCoroutine(VisualFx.Confetti((RectTransform)versusEndPanel.transform, winnerColor));
        }
    }

    private void SetPlayerResult(TextMeshProUGUI text, int player, float money, bool winner)
    {
        if (text == null)
            return;

        OrderManager.RoundStats st = OrderManager.Instance.GetStats(player);
        string combo = st.bestCombo > 1f ? "x" + st.bestCombo.ToString("0.##") : "none";
        string color = ColorUtility.ToHtmlStringRGB(player == 1 ? player1Color : player2Color);

        text.text = $"<color=#{color}>Player {player}{(winner ? " - Winner!" : "")}</color>\n" +
            $"<size=140%>{OrderUIManager.Money(money)}</size>\n" +
            $"{st.served} served\n{st.missed} missed\nBest combo {combo}\n{OrderUIManager.Money(Mathf.Round(st.tips * 100f) / 100f)} in tips";
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        GameModeSelector.retryMode = OrderManager.Instance?.GetCurrentMode();
        MainMenuManager.skipMainMenuOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Menu()
    {
        Time.timeScale = 1f;
        MainMenuManager.skipMainMenuOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
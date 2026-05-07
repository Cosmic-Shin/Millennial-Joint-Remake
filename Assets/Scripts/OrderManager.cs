using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public enum GameMode { TIME, SPEED, VERSUS }

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
    public float strawberryIceCreamPrice = 5f;
    public float bubblegumIceCreamPrice = 5f;
    public float mangoIceCreamPrice = 5f;

    [Header("Normal Economy")]
    public float money = 0f;

    [Header("Versus Economy")]
    public float player1Money = 0f;
    public float player2Money = 0f;

    [Header("Game Mode")]
    public float timeModeDuration = 30f;
    public float timeModeQuota = 100f;
    public float speedModeQuota = 20f;
    public float versusModeDuration = 120f;

    private float currentTime;
    private Order currentOrder;
    private Order player1VersusOrder;
    private Order player2VersusOrder;

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
        player1Money = 0f;
        player2Money = 0f;
        gameEnding = false;
        state = GameState.Playing;

        if (mode == GameMode.VERSUS)
        {
            currentTime = versusModeDuration;

            OrderUIManager.Instance?.ClearOrderImages();
            OrderUIManager.Instance?.HideStatus();

            GenerateVersusOrders();

            VersusUIManager.Instance?.ShowVersusUI();
            VersusUIManager.Instance?.UpdateAllUI();

            OrderUIManager.Instance?.HideNormalGameplayUI();
            VersusUIManager.Instance?.ShowVersusUI();

            Debug.Log("Versus Mode Started");
        }
        else
        {
            VersusUIManager.Instance?.HideVersusUI();

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

            OrderUIManager.Instance?.ShowNormalGameplayUI();
            VersusUIManager.Instance?.HideVersusUI();

            GenerateNewOrder();
            OrderUIManager.Instance?.UpdateGameUI();

            Debug.Log($"Game Mode: {mode} | Goal: ${moneyQuota}");
        }
    }

    private void Update()
    {
        if (state != GameState.Playing || gameEnding)
            return;

        if (currentMode == GameMode.TIME)
        {
            float timeMultiplier = 1f;

            if (PowerUpManager.Instance != null)
                timeMultiplier = PowerUpManager.Instance.GetGameTimerMultiplier();

            currentTime -= Time.deltaTime * timeMultiplier;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                EndGame(money >= moneyQuota);
                return;
            }

            OrderUIManager.Instance?.UpdateGameUI();
        }
        else if (currentMode == GameMode.SPEED)
        {
            currentTime += Time.deltaTime;

            if (money >= moneyQuota)
            {
                EndGame(true);
                return;
            }

            OrderUIManager.Instance?.UpdateGameUI();
        }
        else if (currentMode == GameMode.VERSUS)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                EndVersusGame();
                return;
            }

            VersusUIManager.Instance?.UpdateAllUI();
        }
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

    private void GenerateVersusOrders()
    {
        player1VersusOrder = new Order();
        player1VersusOrder.GenerateRandomOrder();

        player2VersusOrder = new Order();
        player2VersusOrder.GenerateRandomOrder();
    }

    public float TryServeItem(PlayerControl player, KitchenItemData item)
    {
        if (state != GameState.Playing || item == null)
            return 0f;

        if (currentMode == GameMode.VERSUS)
            return TryServeVersusItem(player, item);

        return TryServeNormalItem(item);
    }

    public float TryServeItem(KitchenItemData item)
    {
        return TryServeNormalItem(item);
    }

    private float TryServeNormalItem(KitchenItemData item)
    {
        if (currentOrder == null)
            return 0f;

        OrderItemType? servedType = currentOrder.TryServeItem(item);

        if (servedType == null)
            return 0f;

        float earned = GetPriceForType(servedType.Value);
        money += earned;

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

        return earned;
    }

    private float TryServeVersusItem(PlayerControl player, KitchenItemData item)
    {
        if (player == null)
            return 0f;

        Order targetOrder = player.playerNumber == 1 ? player1VersusOrder : player2VersusOrder;

        if (targetOrder == null)
            return 0f;

        OrderItemType? servedType = targetOrder.TryServeItem(item);

        if (servedType == null)
            return 0f;

        float earned = GetPriceForType(servedType.Value);

        if (player.playerNumber == 1)
            player1Money += earned;
        else
            player2Money += earned;

        if (targetOrder.IsComplete())
        {
            if (player.playerNumber == 1)
            {
                player1VersusOrder = new Order();
                player1VersusOrder.GenerateRandomOrder();
            }
            else
            {
                player2VersusOrder = new Order();
                player2VersusOrder.GenerateRandomOrder();
            }
        }

        VersusUIManager.Instance?.UpdateAllUI();

        return earned;
    }

    private void EndGame(bool won)
    {
        gameEnding = true;
        state = won ? GameState.Won : GameState.Lost;

        OrderUIManager.Instance?.UpdateGameUI();

        ScoreManager.Instance?.ShowGameEndPanel(
            won,
            currentMode,
            money,
            currentTime
        );

        AudioManager.Instance?.PlayGameEndBGM();
    }

    private void EndVersusGame()
    {
        gameEnding = true;
        state = GameState.Won;

        AudioManager.Instance?.PlayGameEndBGM();

        string result;

        if (player1Money > player2Money)
            result = "Player 1 Wins!";
        else if (player2Money > player1Money)
            result = "Player 2 Wins!";
        else
            result = "Draw!";

        VersusUIManager.Instance?.ShowVersusEndPanel(result);
    }

    private void ResetToWaiting()
    {
        state = GameState.Waiting;
        gameEnding = false;
        currentOrder = null;
        player1VersusOrder = null;
        player2VersusOrder = null;
        currentTime = 0f;
        money = 0f;
        player1Money = 0f;
        player2Money = 0f;

        OrderUIManager.Instance?.HideStatus();
        OrderUIManager.Instance?.ClearOrderImages();
        OrderUIManager.Instance?.UpdateGameUI();
        VersusUIManager.Instance?.HideVersusUI();
    }

    public float GetPriceForType(OrderItemType type)
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
            OrderItemType.StrawberryIceCream => strawberryIceCreamPrice,
            OrderItemType.BubblegumIceCream => bubblegumIceCreamPrice,
            OrderItemType.MangoIceCream => mangoIceCreamPrice,
            _ => 0f
        };
    }

    public float GetCurrentTime() => currentTime;
    public Order GetCurrentOrder() => currentOrder;
    public Order GetPlayer1VersusOrder() => player1VersusOrder;
    public Order GetPlayer2VersusOrder() => player2VersusOrder;
    public GameMode GetCurrentMode() => currentMode;
}
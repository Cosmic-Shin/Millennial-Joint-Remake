using UnityEngine;
using System.Collections.Generic;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public enum GameMode { TIME, SPEED, VERSUS }
    private const int ORDER_QUEUE_SIZE = 3;

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
    private List<QueuedOrder> orderQueue = new List<QueuedOrder>();
    private List<QueuedOrder> player1OrderQueue = new List<QueuedOrder>();
    private List<QueuedOrder> player2OrderQueue = new List<QueuedOrder>();

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

        // Clear old queues
        orderQueue.Clear();
        player1OrderQueue.Clear();
        player2OrderQueue.Clear();

        if (mode == GameMode.VERSUS)
        {
            currentTime = versusModeDuration;

            OrderUIManager.Instance?.ClearOrderImages();
            OrderUIManager.Instance?.HideStatus();

            // Initialize both player queues with 3 orders each
            for (int i = 0; i < ORDER_QUEUE_SIZE; i++)
    {
        player1OrderQueue.Add(new QueuedOrder(QueuedOrder.VERSUS_TIMER_DURATION));
        player2OrderQueue.Add(new QueuedOrder(QueuedOrder.VERSUS_TIMER_DURATION));
    }

            VersusUIManager.Instance?.ShowVersusUI();
            VersusUIManager.Instance?.UpdateAllUI();

            OrderUIManager.Instance?.HideNormalGameplayUI();
            VersusUIManager.Instance?.ShowVersusUI();
            OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);

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

            // Initialize normal mode queue with 3 orders
            for (int i = 0; i < ORDER_QUEUE_SIZE; i++)
            {
                orderQueue.Add(new QueuedOrder(QueuedOrder.TIMER_DURATION));
            }

            OrderUIManager.Instance?.ShowNormalGameplayUI();
            VersusUIManager.Instance?.HideVersusUI();
            OrderUIManager.Instance?.UpdateNormalDisplay(orderQueue);
            OrderUIManager.Instance?.UpdateGameUI();

            Debug.Log($"Game Mode: {mode} | Goal: ${moneyQuota}");
        }
    }

    private void Update()
    {
        if (state != GameState.Playing || gameEnding)
            return;

        // Update order queue timers
        if (currentMode == GameMode.VERSUS)
        {
            UpdateQueueTimers(player1OrderQueue);
            UpdateQueueTimers(player2OrderQueue);
        }
        else
        {
            UpdateQueueTimers(orderQueue);
        }

        // Update game timers
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

    private void UpdateQueueTimers(List<QueuedOrder> queue)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            queue[i].UpdateTimer(Time.deltaTime);

            // If order expired, replace it
            if (queue[i].IsExpired())
            {
                float duration = currentMode == GameMode.VERSUS ? QueuedOrder.VERSUS_TIMER_DURATION : QueuedOrder.TIMER_DURATION;
                queue[i] = new QueuedOrder(duration);
                
                if (currentMode == GameMode.VERSUS)
                    OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);
                else
                    OrderUIManager.Instance?.UpdateNormalDisplay(orderQueue);
            }
        }
    }

    private void ShiftOrderQueue(List<QueuedOrder> queue)
    {
        if (queue.Count > 0)
        {
            queue.RemoveAt(0);
            orderQueue.Add(new QueuedOrder(QueuedOrder.TIMER_DURATION));
        }
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
        if (orderQueue.Count == 0)
            return 0f;

        // Try to serve any order in the queue
        for (int i = 0; i < orderQueue.Count; i++)
        {
            if (orderQueue[i].TryServe(item))
            {
                float earned = GetPriceForType(orderQueue[i].item.type);
                money += earned;

                Debug.Log($"Order {i + 1} served! Earned: ${earned}");

                int servedIndex = i;
                OrderUIManager.Instance?.OnOrderServed(servedIndex, () =>
                {
                    orderQueue.RemoveAt(servedIndex);
                    orderQueue.Add(new QueuedOrder(QueuedOrder.TIMER_DURATION));
                });
                OrderUIManager.Instance?.UpdateGameUI();

                if (currentMode == GameMode.SPEED && money >= moneyQuota)
                    EndGame(true);

                return earned;
            }
        }
        return 0f;
    }

    private float TryServeVersusItem(PlayerControl player, KitchenItemData item)
    {
        if (player == null)
            return 0f;

        List<QueuedOrder> targetQueue = player.playerNumber == 1 ? player1OrderQueue : player2OrderQueue;

        if (targetQueue.Count == 0)
            return 0f;

        for (int i = 0; i < targetQueue.Count; i++)
        {
            if (targetQueue[i].TryServe(item))
            {
                float earned = GetPriceForType(targetQueue[i].item.type);

                if (player.playerNumber == 1)
                    player1Money += earned;
                else
                    player2Money += earned;

                Debug.Log($"Player {player.playerNumber} served order {i + 1}! Earned: ${earned}");

                int servedIndex = i;
                OrderUIManager.Instance?.OnVersusOrderServed(player.playerNumber, servedIndex, () =>
                {
                    targetQueue.RemoveAt(servedIndex);
                    targetQueue.Add(new QueuedOrder(QueuedOrder.VERSUS_TIMER_DURATION));
                });
                OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);
                VersusUIManager.Instance?.UpdateAllUI();

                return earned;
            }
        }
        return 0f;
    }

    public void ResetToWaiting()
    {
        state = GameState.Waiting;
        gameEnding = false;
        orderQueue.Clear();
        player1OrderQueue.Clear();
        player2OrderQueue.Clear();
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
    public List<QueuedOrder> GetOrderQueue() => orderQueue;
    public List<QueuedOrder> GetPlayer1OrderQueue() => player1OrderQueue;
    public List<QueuedOrder> GetPlayer2OrderQueue() => player2OrderQueue;
    public GameMode GetCurrentMode() => currentMode;
}
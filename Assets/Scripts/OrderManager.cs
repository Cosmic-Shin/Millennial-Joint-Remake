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

    [Header("Order Rewards")]
    public float expiredOrderPenalty = 2f;
    [Tooltip("Bonus (fraction of the price) for serving while at least this much of the order timer is left")]
    public float fastServeTipPercent = 0.25f;
    [Range(0f, 1f)] public float fastServeTimeFraction = 0.5f;

    [Header("Difficulty Ramp")]
    [Tooltip("New orders get this much less time per minute played (0.1 = 10% shorter each minute)")]
    public float orderTimeShrinkPerMinute = 0.1f;
    [Tooltip("Orders never get shorter than this fraction of their normal time")]
    [Range(0.1f, 1f)] public float minOrderTimeFraction = 0.5f;

    [Header("Combo")]
    [Tooltip("Each serve in a row adds this to the multiplier; an expired order resets it")]
    public float comboStep = 0.25f;
    public float maxComboMultiplier = 2f;

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
    public float countdownDuration = 3f;

    private float currentTime;
    private float countdownRemaining;
    private List<QueuedOrder> orderQueue = new List<QueuedOrder>();
    private List<QueuedOrder> player1OrderQueue = new List<QueuedOrder>();
    private List<QueuedOrder> player2OrderQueue = new List<QueuedOrder>();

    private GameMode currentMode = GameMode.TIME;
    private bool gameEnding;

    public enum GameState { Waiting, Playing, Won, Lost }
    public GameState state = GameState.Waiting;

    public float moneyQuota = 100f;

    public float LastServeTip { get; private set; }
    public float LastServeMultiplier { get; private set; } = 1f;

    private float elapsedTime;
    private readonly int[] combo = new int[3];

    public struct RoundStats
    {
        public int served;
        public int missed;
        public float bestCombo;
        public float tips;
    }
    private readonly RoundStats[] stats = new RoundStats[3];
    public RoundStats GetStats(int slot) => stats[slot];

    private int ComboSlot(int playerNumber) => currentMode == GameMode.VERSUS ? (playerNumber == 2 ? 2 : 1) : 0;

    private QueuedOrder NewOrder()
    {
        float baseDuration = currentMode == GameMode.VERSUS ? QueuedOrder.VERSUS_TIMER_DURATION : QueuedOrder.TIMER_DURATION;
        float fraction = Mathf.Max(minOrderTimeFraction, 1f - orderTimeShrinkPerMinute * elapsedTime / 60f);
        return new QueuedOrder(baseDuration * fraction * MatchRules.OrderTimeMultiplier);
    }

    public bool IsRunning => state == GameState.Playing && countdownRemaining <= 0f;

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
        elapsedTime = 0f;
        System.Array.Clear(combo, 0, combo.Length);
        System.Array.Clear(stats, 0, stats.Length);
        for (int slot = 0; slot < combo.Length; slot++)
            OrderUIManager.Instance?.ShowCombo(slot, 1f);
        countdownRemaining = countdownDuration;
        OrderUIManager.Instance?.ShowCountdown(countdownRemaining);

        if (CameraMovement.Instance != null)
            CameraMovement.Instance.PlayIntro();

        orderQueue.Clear();
        player1OrderQueue.Clear();
        player2OrderQueue.Clear();

        if (mode == GameMode.VERSUS)
        {
            currentTime = versusModeDuration;

            OrderUIManager.Instance?.ClearOrderImages();
            OrderUIManager.Instance?.HideStatus();

            for (int i = 0; i < ORDER_QUEUE_SIZE; i++)
            {
                player1OrderQueue.Add(NewOrder());
                player2OrderQueue.Add(NewOrder());
            }

            OrderUIManager.Instance?.HideNormalGameplayUI();
            VersusUIManager.Instance?.ShowVersusUI();
            OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);
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

            for (int i = 0; i < ORDER_QUEUE_SIZE; i++)
            {
                orderQueue.Add(NewOrder());
            }

            OrderUIManager.Instance?.ShowNormalGameplayUI();
            OrderUIManager.Instance?.UpdateNormalDisplay(orderQueue);
            OrderUIManager.Instance?.UpdateGameUI();
        }
    }

    private void Update()
    {
        if (state != GameState.Playing || gameEnding)
            return;

        if (countdownRemaining > 0f)
        {
            countdownRemaining -= Time.deltaTime;
            OrderUIManager.Instance?.ShowCountdown(countdownRemaining);
            return;
        }

        elapsedTime += Time.deltaTime;

        if (currentMode == GameMode.VERSUS)
        {
            UpdateQueueTimers(player1OrderQueue, 1);
            UpdateQueueTimers(player2OrderQueue, 2);
        }
        else
        {
            UpdateQueueTimers(orderQueue, 1);
        }

        float slow = PowerUpManager.Instance != null ? PowerUpManager.Instance.GetSlowTimeMultiplier(1) : 1f;

        if (currentMode == GameMode.TIME)
        {
            currentTime -= Time.deltaTime * slow;

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
            currentTime += Time.deltaTime * slow;

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

    private void UpdateQueueTimers(List<QueuedOrder> queue, int playerNumber)
    {
        float slow = PowerUpManager.Instance != null ? PowerUpManager.Instance.GetSlowTimeMultiplier(playerNumber) : 1f;

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].IsServed())
                continue;

            queue[i].UpdateTimer(Time.deltaTime * slow);

            if (queue[i].IsExpired())
            {
                int slot = ComboSlot(playerNumber);
                OrderUIManager.Instance?.OnOrderExpired(slot, i, queue[i].item.type, expiredOrderPenalty);
                AudioManager.Instance?.PlayPenaltySFX();
                OrderUIManager.Instance?.BreakCombo(slot);

                queue[i] = NewOrder();
                combo[slot] = 0;
                stats[slot].missed++;
                AddMoney(playerNumber, -expiredOrderPenalty);

                if (currentMode == GameMode.VERSUS)
                    OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);
                else
                    OrderUIManager.Instance?.UpdateNormalDisplay(orderQueue);
            }
        }
    }

    private void AddMoney(int playerNumber, float amount)
    {
        if (currentMode != GameMode.VERSUS)
            money = Mathf.Max(0f, money + amount);
        else if (playerNumber == 1)
            player1Money = Mathf.Max(0f, player1Money + amount);
        else
            player2Money = Mathf.Max(0f, player2Money + amount);

        OrderUIManager.Instance?.FlashMoney(currentMode == GameMode.VERSUS ? playerNumber : 0, amount >= 0f);
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
        string tallyKey;

        if (player1Money > player2Money)
        {
            result = "Player 1 Wins!";
            tallyKey = "VS_P1Wins";
        }
        else if (player2Money > player1Money)
        {
            result = "Player 2 Wins!";
            tallyKey = "VS_P2Wins";
        }
        else
        {
            result = "Draw!";
            tallyKey = "VS_Draws";
        }

        PlayerPrefs.SetInt(tallyKey, PlayerPrefs.GetInt(tallyKey, 0) + 1);
        PlayerPrefs.Save();

        VersusUIManager.Instance?.ShowVersusEndPanel(result);
    }

    public float TryServeItem(PlayerControl player, KitchenItemData item)
    {
        if (!IsRunning || item == null || player == null)
            return 0f;

        bool versus = currentMode == GameMode.VERSUS;
        List<QueuedOrder> queue = !versus ? orderQueue
            : player.playerNumber == 1 ? player1OrderQueue : player2OrderQueue;

        for (int i = 0; i < queue.Count; i++)
        {
            QueuedOrder order = queue[i];

            if (!order.TryServe(item))
                continue;

            float price = GetPriceForType(order.item.type);
            LastServeTip = order.timer >= order.duration * fastServeTimeFraction ? price * fastServeTipPercent : 0f;

            int slot = ComboSlot(player.playerNumber);
            combo[slot]++;
            LastServeMultiplier = Mathf.Min(maxComboMultiplier, 1f + comboStep * (combo[slot] - 1));
            OrderUIManager.Instance?.ShowCombo(slot, LastServeMultiplier);

            float earned = (price + LastServeTip) * LastServeMultiplier;

            stats[slot].served++;
            stats[slot].tips += LastServeTip * LastServeMultiplier;
            stats[slot].bestCombo = Mathf.Max(stats[slot].bestCombo, LastServeMultiplier);

            if (LastServeTip > 0f)
                AudioManager.Instance?.PlayTipSFX();

            AddMoney(player.playerNumber, earned);

            System.Action replace = () =>
            {
                int index = queue.IndexOf(order);
                if (index < 0)
                    return;

                queue.RemoveAt(index);
                queue.Add(NewOrder());
            };

            if (OrderUIManager.Instance == null)
                replace();
            else if (versus)
                OrderUIManager.Instance.OnVersusOrderServed(player.playerNumber, i, replace);
            else
                OrderUIManager.Instance.OnOrderServed(i, replace);

            if (versus)
            {
                OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);
                VersusUIManager.Instance?.UpdateAllUI();
            }
            else
            {
                OrderUIManager.Instance?.UpdateGameUI();

                if (currentMode == GameMode.SPEED && money >= moneyQuota)
                    EndGame(true);
            }

            return earned;
        }

        return 0f;
    }

    public bool ChangeRandomOrder(int playerNumber)
    {
        List<QueuedOrder> queue = playerNumber == 1 ? player1OrderQueue : player2OrderQueue;
        List<QueuedOrder> open = queue.FindAll(o => !o.IsServed());

        if (open.Count == 0)
            return false;

        QueuedOrder order = open[Random.Range(0, open.Count)];
        OrderItemType newType;
        do newType = QueuedOrder.GetRandomItemType();
        while (newType == order.item.type);

        order.item = new OrderItem(newType);

        OrderUIManager.Instance?.UpdateVersusDisplay(player1OrderQueue, player2OrderQueue);
        VersusUIManager.Instance?.UpdateAllUI();
        OrderUIManager.Instance?.FlashChangedOrder(playerNumber, queue.IndexOf(order));
        return true;
    }

    public void ResetToWaiting()
    {
        state = GameState.Waiting;
        gameEnding = false;
        countdownRemaining = 0f;
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
    public QueuedOrder MostUrgentOrder(PlayerControl player, System.Predicate<OrderItemType> matches)
    {
        List<QueuedOrder> queue = currentMode != GameMode.VERSUS || player == null ? orderQueue
            : player.playerNumber == 1 ? player1OrderQueue : player2OrderQueue;

        QueuedOrder best = null;
        foreach (QueuedOrder order in queue)
            if (!order.IsServed() && matches(order.item.type) && (best == null || order.timer < best.timer))
                best = order;

        return best;
    }

    public List<QueuedOrder> GetOrderQueue() => orderQueue;
    public List<QueuedOrder> GetPlayer1OrderQueue() => player1OrderQueue;
    public List<QueuedOrder> GetPlayer2OrderQueue() => player2OrderQueue;
    public GameMode GetCurrentMode() => currentMode;
}

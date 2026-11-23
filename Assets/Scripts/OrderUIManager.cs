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
    public Sprite shrimpTempuraSprite;
    public Sprite chocolateDonutSprite;
    public Sprite strawberryDonutSprite;
    public Sprite galaxyDonutSprite;

    [Header("TMP UI")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    [Header("Normal Gameplay UI Root")]
    public GameObject normalGameplayPanel;

    [Header("Countdown (3-2-1-GO)")]
    public TextMeshProUGUI countdownText;

    [Header("Money Flash")]
    public float moneyFlashTime = 0.6f;

    [Header("Combo Labels (hidden until x1.25+)")]
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI player1ComboText;
    public TextMeshProUGUI player2ComboText;

    [Header("HUD")]
    [Tooltip("The round timer pulses red with this many seconds left")]
    public float timerWarningSeconds = 15f;
    [Tooltip("Tints the fill sprite; white keeps the highlighter's own yellow")]
    public Color goalBarColor = Color.white;
    [Tooltip("Highlighter stroke that fills the money bar")]
    public Sprite goalBarFillSprite;
    public Color goalBarPaperColor = new Color(0.97f, 0.95f, 0.88f);
    public Color goalBarInkColor = new Color(0.12f, 0.1f, 0.15f);

    private string lastCountdownText;
    private RectTransform goalBar;

    public static string Money(float value) =>
        "$" + (Mathf.Approximately(value, Mathf.Round(value)) ? value.ToString("0") : value.ToString("0.00"));

    public static string Clock(float seconds)
    {
        int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return s / 60 + ":" + (s % 60).ToString("00");
    }

    public static string Summary(OrderManager.RoundStats s)
    {
        string combo = s.bestCombo > 1f ? "x" + s.bestCombo.ToString("0.##") : "none";
        return $"{s.served} served · {s.missed} missed · best combo {combo} · {Money(Mathf.Round(s.tips * 100f) / 100f)} in tips";
    }

    public static void SetRoundTimer(TMP_Text text, float seconds, bool countdown, float warningSeconds)
    {
        text.text = Clock(seconds);

        bool urgent = countdown && seconds > 0f && seconds <= warningSeconds;
        float pulse = urgent ? 0.5f + 0.5f * Mathf.Sin(Time.time * 10f) : 0f;
        text.color = urgent ? Color.Lerp(new Color(1f, 0.55f, 0.5f), new Color(1f, 0.15f, 0.1f), pulse) : Color.white;
        text.transform.localScale = Vector3.one * (1f + 0.12f * pulse);
    }

    public static Color PatienceColor(float fill) => fill > 0.5f
        ? Color.Lerp(Color.yellow, new Color(0.3f, 1f, 0.35f), (fill - 0.5f) * 2f)
        : Color.Lerp(new Color(1f, 0.2f, 0.15f), Color.yellow, fill * 2f);

    // A strip of paper with an ink border, filled by a highlighter stroke, to match the tickets.
    private RectTransform MakeBar(Transform parent, Vector2 position, Vector2 size, int siblingIndex)
    {
        var back = new GameObject("Bar", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
        back.SetParent(parent, false);
        back.SetSiblingIndex(siblingIndex);
        back.anchoredPosition = position;
        back.sizeDelta = size;
        var backImage = back.GetComponent<Image>();
        backImage.color = goalBarPaperColor;
        backImage.raycastTarget = false;
        var ink = back.GetComponent<Outline>();
        ink.effectColor = goalBarInkColor;
        ink.effectDistance = new Vector2(3f, 3f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        fill.SetParent(back, false);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(4f, 3f);
        fill.offsetMax = new Vector2(-4f, -3f);
        var fillImage = fill.GetComponent<Image>();
        fillImage.sprite = goalBarFillSprite;
        fillImage.raycastTarget = false;
        return fill;
    }

    private static void SetBar(RectTransform fill, float amount, Color color)
    {
        amount = Mathf.Clamp01(amount);
        fill.anchorMax = new Vector2(amount, 1f);
        fill.gameObject.SetActive(amount > 0.02f);
        fill.GetComponent<Image>().color = color;
    }

    private int changingOrders;
    private int changingPlayer1Orders;
    private int changingPlayer2Orders;
    private readonly System.Collections.Generic.Dictionary<TMP_Text, Color> moneyTextColors =
        new System.Collections.Generic.Dictionary<TMP_Text, Color>();

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
        HideCountdown();

        for (int slot = 0; slot < 3; slot++)
            ShowCombo(slot, 1f);
    }

    public void ShowCountdown(float remaining)
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);
        string text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "GO!";

        if (text != lastCountdownText)
        {
            lastCountdownText = text;
            countdownText.text = text;
            countdownText.color = remaining > 0f ? Color.white : Color.green;
            StartCoroutine(VisualFx.Pop(countdownText.transform, 2.2f, 0.4f));

            if (remaining > 0f)
                AudioManager.Instance?.PlayCountdownBeep();
            else
                AudioManager.Instance?.PlayGoHorn();
        }

        if (remaining <= 0f)
        {
            CancelInvoke(nameof(HideCountdown));
            Invoke(nameof(HideCountdown), 0.7f);
        }
    }

    private void HideCountdown()
    {
        lastCountdownText = null;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private TextMeshProUGUI ComboText(int slot) =>
        slot == 0 ? comboText : slot == 1 ? player1ComboText : player2ComboText;

    public void ShowCombo(int slot, float multiplier)
    {
        TextMeshProUGUI text = ComboText(slot);
        if (text == null)
            return;

        if (multiplier <= 1f)
        {
            text.gameObject.SetActive(false);
            return;
        }

        text.gameObject.SetActive(true);
        text.text = "x" + multiplier.ToString("0.##") + " COMBO";
        text.color = Color.yellow;
        StartCoroutine(VisualFx.Pop(text.transform, 1.6f, 0.3f));
        AudioManager.Instance?.PlayComboUpSFX(multiplier);
    }

    public void BreakCombo(int slot)
    {
        TextMeshProUGUI text = ComboText(slot);
        if (text != null && text.gameObject.activeInHierarchy)
            StartCoroutine(BreakComboRoutine(text));
    }

    private IEnumerator BreakComboRoutine(TextMeshProUGUI text)
    {
        text.text = "COMBO LOST";
        text.color = Color.red;
        AudioManager.Instance?.PlayComboLostSFX();
        yield return VisualFx.Shake(text.rectTransform, 18f, 0.6f);

        if (text.text == "COMBO LOST")
            text.gameObject.SetActive(false);
    }

    private Image GetOrderImage(int slot, int index)
    {
        Image[] images = slot == 0 ? new[] { order1Image, order2Image, order3Image }
            : VersusUIManager.Instance == null ? null
            : slot == 1 ? new[] { VersusUIManager.Instance.p1Order1Image, VersusUIManager.Instance.p1Order2Image, VersusUIManager.Instance.p1Order3Image }
            : new[] { VersusUIManager.Instance.p2Order1Image, VersusUIManager.Instance.p2Order2Image, VersusUIManager.Instance.p2Order3Image };

        return images != null && index >= 0 && index < images.Length ? images[index] : null;
    }

    public void OnOrderExpired(int slot, int index, OrderItemType expiredType, float penalty)
    {
        Image image = GetOrderImage(slot, index);
        if (image == null || !image.isActiveAndEnabled)
            return;

        StartCoroutine(VisualFx.DropGhost(image, GetSpriteForOrder(expiredType)));

        if (penalty > 0f)
        {
            var rect = (RectTransform)image.transform;
            StartCoroutine(VisualFx.FloatText((RectTransform)rect.parent, rect.anchoredPosition + Vector2.down * 120f,
                "-$" + penalty.ToString("0.##"), moneyText != null ? moneyText.font : null, Color.red));
        }
    }

    public void FlashChangedOrder(int playerNumber, int index)
    {
        Image image = GetOrderImage(playerNumber, index);
        if (image == null || !image.isActiveAndEnabled)
            return;

        StartCoroutine(VisualFx.FlashImage(image, Color.yellow, 0.8f));

        var rect = (RectTransform)image.transform;
        StartCoroutine(VisualFx.FloatText((RectTransform)rect.parent, rect.anchoredPosition + Vector2.down * 120f,
            "CHANGED!", moneyText != null ? moneyText.font : null, Color.yellow, 50f));
    }

    private TMP_Text MoneyTextFor(int playerNumber) => playerNumber == 0 ? moneyText
        : VersusUIManager.Instance == null ? null
        : playerNumber == 1 ? VersusUIManager.Instance.player1MoneyText
        : VersusUIManager.Instance.player2MoneyText;

    [Header("Coin Fly")]
    public Color coinColor = new Color(1f, 0.85f, 0.25f);
    public float coinFlyTime = 0.55f;

    private readonly System.Collections.Generic.Dictionary<TMP_Text, Vector3> moneyTextScales =
        new System.Collections.Generic.Dictionary<TMP_Text, Vector3>();

    public void FlyCoins(Vector3 worldPosition, int playerNumber, int count)
    {
        TMP_Text target = MoneyTextFor(playerNumber);
        Camera cam = Camera.main;
        if (target == null || !target.isActiveAndEnabled || cam == null)
            return;

        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z < 0f)
            return;

        for (int i = 0; i < count; i++)
            StartCoroutine(FlyCoinRoutine(screen, target, i * 0.05f));
    }

    private IEnumerator FlyCoinRoutine(Vector3 from, TMP_Text target, float delay)
    {
        for (float d = 0f; d < delay; d += Time.unscaledDeltaTime)
            yield return null;

        Transform root = target.canvas.rootCanvas.transform;
        var go = new GameObject("Coin", typeof(RectTransform));
        go.transform.SetParent(root, false);
        var coin = go.AddComponent<TextMeshProUGUI>();
        coin.text = "$";
        coin.font = target.font;
        coin.fontSize = 70f;
        coin.color = coinColor;
        coin.alignment = TextAlignmentOptions.Center;
        coin.raycastTarget = false;
        ((RectTransform)go.transform).sizeDelta = new Vector2(100f, 100f);

        Vector3 start = from + (Vector3)(Random.insideUnitCircle * 40f);
        Vector3 control = start + new Vector3(Random.Range(-160f, 160f), Random.Range(120f, 260f), 0f);

        for (float e = 0f; e < coinFlyTime && target != null; e += Time.unscaledDeltaTime)
        {
            float k = e / coinFlyTime;
            float eased = k * k;
            Vector3 end = target.transform.position;
            go.transform.position = Vector3.Lerp(Vector3.Lerp(start, control, eased), Vector3.Lerp(control, end, eased), eased);
            go.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.6f, k);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * 25f);
            yield return null;
        }

        Destroy(go);

        if (target == null)
            yield break;

        if (!moneyTextScales.TryGetValue(target, out Vector3 baseScale))
            moneyTextScales[target] = baseScale = target.transform.localScale;

        StartCoroutine(VisualFx.Pop(target.transform, 1.18f, 0.18f, baseScale));
        AudioManager.Instance?.PlayCoinTick();
    }

    public void FlashMoney(int playerNumber, bool gained)
    {
        TMP_Text text = MoneyTextFor(playerNumber);

        if (text != null && text.isActiveAndEnabled)
            StartCoroutine(FlashMoneyRoutine(text, gained ? Color.green : Color.red));
    }

    private IEnumerator FlashMoneyRoutine(TMP_Text text, Color flashColor)
    {
        if (!moneyTextColors.TryGetValue(text, out Color normal))
            moneyTextColors[text] = normal = text.color;

        for (float t = 0f; t < moneyFlashTime; t += Time.deltaTime)
        {
            text.color = Color.Lerp(flashColor, normal, t / moneyFlashTime);
            yield return null;
        }

        text.color = normal;
    }

    private void Update()
    {
        if (OrderManager.Instance == null || OrderManager.Instance.state != OrderManager.GameState.Playing)
            return;

        if (OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS)
        {
            UpdateVersusDisplay(OrderManager.Instance.GetPlayer1OrderQueue(), OrderManager.Instance.GetPlayer2OrderQueue());

            VersusUIManager v = VersusUIManager.Instance;
            if (v != null)
            {
                if (changingPlayer1Orders == 0)
                    UpdatePatience(Imgs(v.p1Order1Image, v.p1Order2Image, v.p1Order3Image), OrderManager.Instance.GetPlayer1OrderQueue());
                if (changingPlayer2Orders == 0)
                    UpdatePatience(Imgs(v.p2Order1Image, v.p2Order2Image, v.p2Order3Image), OrderManager.Instance.GetPlayer2OrderQueue());
            }
        }
        else
        {
            UpdateNormalDisplay(OrderManager.Instance.GetOrderQueue());

            if (changingOrders == 0)
                UpdatePatience(Imgs(order1Image, order2Image, order3Image), OrderManager.Instance.GetOrderQueue());
        }

        if (GatherWorldUiRects())
        {
            VersusUIManager vs = VersusUIManager.Instance;
            FadeTicketsOverWorldUi(order1Image, order2Image, order3Image);
            if (vs != null)
                FadeTicketsOverWorldUi(vs.p1Order1Image, vs.p1Order2Image, vs.p1Order3Image, vs.p2Order1Image, vs.p2Order2Image, vs.p2Order3Image);
        }
    }

    [Tooltip("Ticket opacity while in-world UI (cook timers, ingredient picker, player text) is behind it")]
    public float coveredTicketAlpha = 0.2f;
    private readonly Vector3[] corners = new Vector3[8];
    private readonly System.Collections.Generic.List<Rect> popupRects = new System.Collections.Generic.List<Rect>();
    private readonly System.Collections.Generic.List<Canvas> worldCanvases = new System.Collections.Generic.List<Canvas>();
    private TextMesh[] worldTexts = new TextMesh[0];
    private float nextWorldUiScan;

    // Overlay tickets always draw on top of in-world UI, so we find what's on screen and fade tickets hiding it.
    private bool GatherWorldUiRects()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return false;

        if (Time.unscaledTime >= nextWorldUiScan) // maps and floating texts come and go, so rescan a few times a second
        {
            nextWorldUiScan = Time.unscaledTime + 0.25f;
            worldCanvases.Clear();
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.isRootCanvas && c.renderMode == RenderMode.WorldSpace)
                    worldCanvases.Add(c);
            worldTexts = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
        }

        popupRects.Clear();
        foreach (Canvas c in worldCanvases)
            if (c != null && c.isActiveAndEnabled)
            {
                ((RectTransform)c.transform).GetWorldCorners(corners);
                AddScreenRect(cam, 4);
            }
        foreach (TextMesh t in worldTexts)
            if (t != null && t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text) && t.TryGetComponent(out Renderer r))
            {
                Bounds b = r.bounds;
                for (int i = 0; i < 8; i++)
                    corners[i] = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) * 2 - 1, (i & 2) - 1, (i & 4) / 2f - 1));
                AddScreenRect(cam, 8);
            }
        return true;
    }

    private void AddScreenRect(Camera cam, int count)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
        for (int i = 0; i < count; i++)
        {
            Vector3 s = cam.WorldToScreenPoint(corners[i]);
            if (s.z < 0f)
                return; // behind the camera
            min = Vector2.Min(min, s);
            max = Vector2.Max(max, s);
        }
        popupRects.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
    }

    private void FadeTicketsOverWorldUi(params Image[] tickets)
    {
        foreach (Image ticket in tickets)
        {
            if (ticket == null || !ticket.gameObject.activeInHierarchy)
                continue;
            ticket.rectTransform.GetWorldCorners(corners); // overlay canvas: corners are already screen pixels
            Rect r = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            bool covered = popupRects.Exists(p => p.Overlaps(r));
            if (!ticket.TryGetComponent(out CanvasGroup group))
                group = ticket.gameObject.AddComponent<CanvasGroup>();
            group.alpha = Mathf.MoveTowards(group.alpha, covered ? coveredTicketAlpha : 1f, Time.unscaledDeltaTime * 6f);
        }
    }

    [Tooltip("Tickets wobble when this many seconds are left")]
    public float patienceWobbleSeconds = 10f;

    private void UpdatePatience(Image[] tickets, System.Collections.Generic.List<QueuedOrder> queue)
    {
        for (int i = 0; i < tickets.Length; i++)
        {
            Image ticket = tickets[i];
            if (ticket == null)
                continue;

            QueuedOrder order = i < queue.Count ? queue[i] : null;
            float urgency = order != null && !order.IsServed() && ticket.enabled
                ? 1f - Mathf.Clamp01(order.timer / patienceWobbleSeconds)
                : 0f;

            ticket.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 6f * urgency);

            // Reddens over twice the wobble window, pulsing harder as it runs out.
            float heat = order != null && !order.IsServed() && ticket.enabled
                ? 1f - Mathf.Clamp01(order.timer / (patienceWobbleSeconds * 2f))
                : 0f;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * (6f + 10f * heat));
            ticket.color = Color.Lerp(Color.white, urgentTicketColor, heat * pulse);
        }
    }

    public Color urgentTicketColor = new Color(1f, 0.55f, 0.5f);

    public void UpdateNormalDisplay(System.Collections.Generic.List<QueuedOrder> queue)
    {
        if (changingOrders > 0)
            return;

        Image[] images = Imgs(order1Image, order2Image, order3Image);
        TextMeshProUGUI[] timerTexts = Texts(order1TimerText, order2TimerText, order3TimerText);

        for (int i = 0; i < 3; i++)
        {
            if (i < queue.Count && queue[i] != null)
            {
                if (images[i] != null)
                {
                    images[i].sprite = GetSpriteForOrder(queue[i].item.type);
                    images[i].enabled = true;
                }

                if (timerTexts[i] != null)
                {
                    int timerDisplayValue = Mathf.CeilToInt(Mathf.Max(0, queue[i].timer));
                    timerTexts[i].text = Num(timerDisplayValue);
                    timerTexts[i].color = queue[i].timer <= 10f ? Color.red : Color.white;
                }
            }
            else
            {
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
        if (changingPlayer1Orders == 0)
            UpdateTimerTexts(player1Queue, Texts(player1Order1TimerText, player1Order2TimerText, player1Order3TimerText));

        if (changingPlayer2Orders == 0)
            UpdateTimerTexts(player2Queue, Texts(player2Order1TimerText, player2Order2TimerText, player2Order3TimerText));
    }

    // Per-frame UI used to allocate fresh arrays and number strings every frame; reuse them instead.
    private readonly Image[] imageBuffer = new Image[3];
    private readonly TextMeshProUGUI[] textBuffer = new TextMeshProUGUI[3];
    private static readonly string[] numberStrings = new string[1000];

    private Image[] Imgs(Image a, Image b, Image c) { imageBuffer[0] = a; imageBuffer[1] = b; imageBuffer[2] = c; return imageBuffer; }
    private TextMeshProUGUI[] Texts(TextMeshProUGUI a, TextMeshProUGUI b, TextMeshProUGUI c) { textBuffer[0] = a; textBuffer[1] = b; textBuffer[2] = c; return textBuffer; }

    private static string Num(int n) =>
        n >= 0 && n < numberStrings.Length ? numberStrings[n] ??= n.ToString() : n.ToString();

    private void UpdateTimerTexts(System.Collections.Generic.List<QueuedOrder> queue, TextMeshProUGUI[] timerTexts)
    {
        for (int i = 0; i < 3; i++)
        {
            if (timerTexts[i] == null)
                continue;

            if (i < queue.Count && queue[i] != null)
            {
                timerTexts[i].text = Num(Mathf.CeilToInt(Mathf.Max(0, queue[i].timer)));
                timerTexts[i].color = queue[i].timer <= 10f ? Color.red : Color.white;
            }
            else
            {
                timerTexts[i].text = "";
            }
        }
    }

    public void OnOrderServed(int index, System.Action onComplete)
    {
        StartCoroutine(ShiftOrdersCoroutine(index, onComplete));
    }

    private IEnumerator ShiftOrdersCoroutine(int index, System.Action onComplete)
    {
        changingOrders++;

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

        changingOrders--;
    }

    public void OnVersusOrderServed(int playerNumber, int index, System.Action onComplete)
    {
        StartCoroutine(ShiftVersusOrdersCoroutine(playerNumber, onComplete));
    }

    private IEnumerator ShiftVersusOrdersCoroutine(int playerNumber, System.Action onComplete)
    {
        if (playerNumber == 1) changingPlayer1Orders++;
        else changingPlayer2Orders++;

        yield return new WaitForSeconds(servedIndicatorStayTime + servedIndicatorFadeTime);

        onComplete?.Invoke();

        if (playerNumber == 1) changingPlayer1Orders--;
        else changingPlayer2Orders--;
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
            OrderItemType.ShrimpTempura => shrimpTempuraSprite,
            OrderItemType.ChocolateDonut => chocolateDonutSprite,
            OrderItemType.StrawberryDonut => strawberryDonutSprite,
            OrderItemType.GalaxyDonut => galaxyDonutSprite,
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
            SetRoundTimer(timerText, OrderManager.Instance.GetCurrentTime(),
                OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.TIME, timerWarningSeconds);

        UpdateMoneyDisplay();
    }

    public void UpdateMoneyDisplay()
    {
        if (moneyText == null)
            return;

        float money = OrderManager.Instance != null ? OrderManager.Instance.money : 0f;
        float quota = OrderManager.Instance != null ? OrderManager.Instance.moneyQuota : 0f;
        moneyText.text = Money(money) + "<size=60%> / " + Money(quota) + "</size>";

        if (goalText == null)
            return;

        if (goalBar == null)
        {
            var rt = goalText.rectTransform;
            goalBar = MakeBar(rt.parent, rt.anchoredPosition, new Vector2(rt.sizeDelta.x, 34f), rt.GetSiblingIndex());
            goalText.gameObject.SetActive(false);
        }

        // Story: the bar runs up to the 3-star mark, with a star under each target.
        float barEnd = quota * (StoryMode.IsActive ? StoryMode.ThreeStarRatio : 1f);
        SetBar(goalBar, barEnd > 0f ? money / barEnd : 0f, quota > 0f && money >= quota ? new Color(0.55f, 1f, 0.5f) : goalBarColor);

        if (StoryMode.IsActive && quota > 0f)
            UpdateGoalStars(money / quota);
    }

    private static readonly float[] StarMarks = { 1f, StoryMode.TwoStarRatio, StoryMode.ThreeStarRatio };
    private Image[] goalStars;
    private int goalStarsLit = -1, goalStarsBest;

    private void UpdateGoalStars(float ratio)
    {
        ScoreManager score = ScoreManager.Instance;
        if (goalBar == null || score == null || score.starImages[0] == null)
            return;

        if (goalStars == null)
        {
            goalStars = new Image[StarMarks.Length];
            for (int i = 0; i < goalStars.Length; i++)
            {
                var star = new GameObject("Goal Star", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                RectTransform r = star.rectTransform;
                r.SetParent(goalBar.parent, false);
                r.anchorMin = r.anchorMax = new Vector2(StarMarks[i] / StoryMode.ThreeStarRatio, 0f);
                r.pivot = new Vector2(1f, 1f); // right edge on the mark, so the last star stays on screen
                r.anchoredPosition = new Vector2(0f, -5f);
                r.sizeDelta = new Vector2(36f, 36f);
                star.sprite = score.starImages[0].sprite;
                star.preserveAspect = true;
                star.raycastTarget = false;
                goalStars[i] = star;
            }
        }

        int lit = 0;
        while (lit < StarMarks.Length && ratio >= StarMarks[lit])
            lit++;

        if (lit == goalStarsLit)
            return;
        goalStarsLit = lit;

        for (int i = 0; i < goalStars.Length; i++)
            goalStars[i].color = i < lit ? score.starOnColor : goalBarPaperColor;

        // Only celebrate a star once, even if a missed order drops below it and it's earned back.
        if (lit <= goalStarsBest)
            return;
        goalStarsBest = lit;

        StartCoroutine(VisualFx.Pop(goalStars[lit - 1].transform, 2.6f, 0.35f));
        AudioManager.Instance?.PlayStoryStarSFX(lit - 1);
        if (lit == 1)
            StartCoroutine(VisualFx.FloatText((RectTransform)moneyText.transform.parent,
                moneyText.rectTransform.anchoredPosition + Vector2.down * 230f, "GOAL!", moneyText.font, Color.yellow, 64f));
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
        foreach (Image ticket in new[] { order1Image, order2Image, order3Image })
            if (ticket != null)
                ticket.rectTransform.localRotation = Quaternion.identity;

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
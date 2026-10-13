using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchRules : MonoBehaviour
{
    public static MatchRules Instance { get; private set; }

    public static float OrderTimeMultiplier = 1f;
    public static float MoveSpeedMultiplier = 1f;
    public static bool UnlimitedStamina;
    public static float PowerUpIntervalMultiplier = 1f;

    [Header("Panel")]
    public GameObject panel;
    public TMP_Text title;
    [Tooltip("Inactive row to clone: children Label (TMP), Prev (Button), Value (TMP), Next (Button)")]
    public RectTransform rowTemplate;
    public float firstRowY = 230f;
    public float rowSpacing = 105f;
    public Button openButton;
    public Button doneButton;
    public Button resetButton;
    public Button backButton;
    [Tooltip("Shown again when the rules panel closes")]
    public GameObject returnPanel;

    private class Rule
    {
        public string label;
        public string key;
        public float[] values;
        public string[] names;
        public float defaultValue;
        public int index;
        public RectTransform row;
        public TMP_Text valueText;

        public float Value => values[index];
        public bool IsDefault => Mathf.Approximately(Value, defaultValue);
    }

    private readonly List<Rule> rules = new List<Rule>();

    private float defaultTimeDuration, defaultVersusDuration, defaultTimeGoal, defaultSpeedGoal;
    private bool defaultsCached;

    private void CacheDefaults()
    {
        OrderManager om = OrderManager.Instance != null ? OrderManager.Instance : FindFirstObjectByType<OrderManager>();
        if (defaultsCached || om == null)
            return;

        defaultTimeDuration = om.timeModeDuration;
        defaultVersusDuration = om.versusModeDuration;
        defaultTimeGoal = om.timeModeQuota;
        defaultSpeedGoal = om.speedModeQuota;
        defaultsCached = true;
    }

    private void Awake()
    {
        Instance = this;
        CacheDefaults();
        if (panel != null)
            panel.SetActive(false);
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (openButton != null) openButton.onClick.AddListener(() => Open(GameModeSelector.Instance != null ? GameModeSelector.Instance.PendingMode : OrderManager.GameMode.TIME));
        if (doneButton != null) doneButton.onClick.AddListener(Close);
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
    }

    private static string Minutes(float s) => s % 60f == 0f ? (s / 60f) + " min" : Mathf.RoundToInt(s) + " sec";
    private static string[] Names(float[] values, System.Func<float, string> format) => System.Array.ConvertAll(values, v => format(v));

    private void BuildRules(OrderManager.GameMode forMode)
    {
        foreach (Rule r in rules)
            if (r.row != null)
                Destroy(r.row.gameObject);
        rules.Clear();

        CacheDefaults();
        string m = forMode.ToString();

        if (forMode != OrderManager.GameMode.SPEED)
        {
            float[] times = { 60f, 120f, 180f, 300f, 420f, 600f };
            Add("Round Time", "Rules_" + m + "_Time", times, Names(times, Minutes),
                forMode == OrderManager.GameMode.TIME ? defaultTimeDuration : defaultVersusDuration);
        }

        if (forMode != OrderManager.GameMode.VERSUS)
        {
            float[] goals = { 20f, 50f, 75f, 100f, 150f, 200f, 300f, 500f };
            Add("Money Goal", "Rules_" + m + "_Goal", goals, Names(goals, v => "$" + v),
                forMode == OrderManager.GameMode.TIME ? defaultTimeGoal : defaultSpeedGoal);
        }

        float[] patience = { 0.5f, 0.75f, 1f, 1.5f, 2f };
        Add("Order Patience", "Rules_Patience", patience, new[] { "Very Short", "Short", "Normal", "Long", "Very Long" }, 1f);

        float[] speed = { 0.75f, 1f, 1.25f, 1.5f };
        Add("Player Speed", "Rules_Speed", speed, new[] { "Slow", "Normal", "Fast", "Very Fast" }, 1f);

        Add("Unlimited Stamina", "Rules_Stamina", new[] { 0f, 1f }, new[] { "Off", "On" }, 0f);

        float[] drops = { 0f, 2f, 1f, 0.5f, 0.25f };
        Add("Power-Up Drops", "Rules_PowerUps", drops, new[] { "Off", "Rare", "Normal", "Often", "Frantic" }, 1f);
    }

    private void Add(string label, string key, float[] values, string[] names, float defaultValue)
    {
        int defaultIndex = 0;
        for (int i = 1; i < values.Length; i++)
            if (Mathf.Abs(values[i] - defaultValue) < Mathf.Abs(values[defaultIndex] - defaultValue))
                defaultIndex = i;

        var rule = new Rule
        {
            label = label, key = key, values = values, names = names,
            defaultValue = values[defaultIndex],
            index = Mathf.Clamp(PlayerPrefs.GetInt(key, defaultIndex), 0, values.Length - 1)
        };
        rules.Add(rule);
    }

    public void Open(OrderManager.GameMode forMode)
    {
        BuildRules(forMode);

        if (title != null)
            title.color = Color.white;
        if (title != null)
            title.text = "Match Rules - " + (forMode == OrderManager.GameMode.TIME ? "Time" : forMode == OrderManager.GameMode.SPEED ? "Speed" : "Versus") + " Mode";

        for (int i = 0; i < rules.Count; i++)
        {
            Rule rule = rules[i];
            rule.row = Instantiate(rowTemplate, rowTemplate.parent);
            rule.row.name = rule.label;
            rule.row.anchoredPosition = new Vector2(rowTemplate.anchoredPosition.x, firstRowY - i * rowSpacing);
            rule.row.gameObject.SetActive(true);

            rule.row.Find("Label").GetComponent<TMP_Text>().text = rule.label;
            rule.valueText = rule.row.Find("Value").GetComponent<TMP_Text>();
            rule.row.Find("Prev").GetComponent<Button>().onClick.AddListener(() => Step(rule, -1));
            rule.row.Find("Next").GetComponent<Button>().onClick.AddListener(() => Step(rule, 1));
            Refresh(rule);
        }

        if (returnPanel != null)
            returnPanel.SetActive(false);
        panel.SetActive(true);
    }

    private void Step(Rule rule, int direction)
    {
        rule.index = (rule.index + direction + rule.values.Length) % rule.values.Length;
        PlayerPrefs.SetInt(rule.key, rule.index);
        Refresh(rule);
        AudioManager.Instance?.PlayChalkHoverSFX();
    }

    private void Refresh(Rule rule)
    {
        rule.valueText.text = rule.names[rule.index];
        rule.valueText.color = rule.IsDefault ? Color.white : new Color(1f, 0.85f, 0.3f);
    }

    private void ResetToDefaults()
    {
        foreach (Rule rule in rules)
        {
            rule.index = System.Array.IndexOf(rule.values, rule.defaultValue);
            PlayerPrefs.DeleteKey(rule.key);
            Refresh(rule);
        }
    }

    private void Close()
    {
        PlayerPrefs.Save();
        panel.SetActive(false);
        if (returnPanel != null)
            returnPanel.SetActive(true);
        GameModeSelector.Instance?.FillModeDescriptions();
    }

    public void Apply(OrderManager.GameMode forMode)
    {
        BuildRules(forMode);
        OrderManager om = OrderManager.Instance;
        if (om == null)
            return;

        OrderTimeMultiplier = 1f;
        MoveSpeedMultiplier = 1f;
        UnlimitedStamina = false;
        PowerUpIntervalMultiplier = 1f;

        foreach (Rule rule in rules)
        {
            switch (rule.label)
            {
                case "Round Time":
                    if (forMode == OrderManager.GameMode.TIME) om.timeModeDuration = rule.Value;
                    else om.versusModeDuration = rule.Value;
                    break;
                case "Money Goal":
                    if (forMode == OrderManager.GameMode.TIME) om.timeModeQuota = rule.Value;
                    else om.speedModeQuota = rule.Value;
                    break;
                case "Order Patience": OrderTimeMultiplier = rule.Value; break;
                case "Player Speed": MoveSpeedMultiplier = rule.Value; break;
                case "Unlimited Stamina": UnlimitedStamina = rule.Value > 0f; break;
                case "Power-Up Drops": PowerUpIntervalMultiplier = rule.Value; break;
            }
        }
    }
}

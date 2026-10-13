using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameModeSelector : MonoBehaviour
{
    public static GameModeSelector Instance { get; private set; }

    public GameObject modePanel;
    public Button timeModeBut;
    public Button speedModeBut;
    public Button versusModeBut;

    [Header("Mode Card Descriptions (filled from OrderManager's settings)")]
    public TMP_Text timeModeDesc;
    public TMP_Text speedModeDesc;
    public TMP_Text versusModeDesc;

    [Header("Best Star Rating per Mode")]
    public Image[] timeBestStars;
    public Image[] speedBestStars;
    public Color bestStarOnColor = new Color(1f, 0.85f, 0.2f);
    public Color bestStarOffColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("Choose Control Panel")]
    public GameObject controlPanel;
    public TMP_Text p1DeviceText;
    public TMP_Text p2DeviceText;
    public TMP_Text joystickStatusText;
    public Button p1PrevBut;
    public Button p1NextBut;
    public Button p2PrevBut;
    public Button p2NextBut;
    public Button startBut;
    public Button controlBackBut;
    public Image p1DeviceIcon;
    public Image p2DeviceIcon;
    public Sprite keyboardIcon;
    public Sprite joystickIcon;

    private static ControlScheme[] chosenSchemes;

    public static OrderManager.GameMode? retryMode;

    private OrderManager.GameMode pendingMode;
    public OrderManager.GameMode PendingMode => pendingMode;
    private Vector3? startBaseScale;
    private readonly List<int> connectedJoysticks = new List<int>();

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
        if (timeModeBut != null)
            timeModeBut.onClick.AddListener(() => SelectMode(OrderManager.GameMode.TIME));

        if (speedModeBut != null)
            speedModeBut.onClick.AddListener(() => SelectMode(OrderManager.GameMode.SPEED));

        if (versusModeBut != null)
            versusModeBut.onClick.AddListener(() => SelectMode(OrderManager.GameMode.VERSUS));

        FillModeDescriptions();

        ShowBestStars(timeBestStars, ScoreManager.GetBestStars(OrderManager.GameMode.TIME));
        ShowBestStars(speedBestStars, ScoreManager.GetBestStars(OrderManager.GameMode.SPEED));

        if (p1PrevBut != null) p1PrevBut.onClick.AddListener(() => ToggleScheme(0));
        if (p1NextBut != null) p1NextBut.onClick.AddListener(() => ToggleScheme(0));
        if (p2PrevBut != null) p2PrevBut.onClick.AddListener(() => ToggleScheme(1));
        if (p2NextBut != null) p2NextBut.onClick.AddListener(() => ToggleScheme(1));

        if (startBut != null)
            startBut.onClick.AddListener(StartWithChosenControls);

        if (controlBackBut != null)
            controlBackBut.onClick.AddListener(BackToModeSelect);

        if (controlPanel != null)
            controlPanel.SetActive(false);
    }

    public void FillModeDescriptions()
    {
        OrderManager om = OrderManager.Instance;
        if (om == null)
            return;

        if (MatchRules.Instance != null)
        {
            MatchRules.Instance.Apply(OrderManager.GameMode.TIME);
            MatchRules.Instance.Apply(OrderManager.GameMode.SPEED);
            MatchRules.Instance.Apply(OrderManager.GameMode.VERSUS);
        }

        if (timeModeDesc != null)
            timeModeDesc.text = $"Earn ${om.timeModeQuota:0} in {Duration(om.timeModeDuration)}";

        if (speedModeDesc != null)
            speedModeDesc.text = $"Reach ${om.speedModeQuota:0} as fast as you can";

        if (versusModeDesc != null)
            versusModeDesc.text = $"{Duration(om.versusModeDuration)} head to head. Most money wins!";
    }

    private void ShowBestStars(Image[] stars, int earned)
    {
        if (stars == null)
            return;

        for (int i = 0; i < stars.Length; i++)
            if (stars[i] != null)
                stars[i].color = i < earned ? bestStarOnColor : bestStarOffColor;
    }

    private static string Duration(float seconds)
    {
        int s = Mathf.RoundToInt(seconds);
        if (s >= 60 && s % 60 == 0)
            return s == 60 ? "1 minute" : $"{s / 60} minutes";
        return $"{s} seconds";
    }

    private void Update()
    {
        if (controlPanel != null && controlPanel.activeInHierarchy)
            RefreshControlPanel();
    }

    public void SelectMode(OrderManager.GameMode mode)
    {
        pendingMode = mode;

        if (controlPanel == null)
        {
            StartMode(mode);
            return;
        }

        if (modePanel != null)
            modePanel.SetActive(false);

        ScanJoysticks();

        if (chosenSchemes == null)
        {
            ControlScheme p2Default = connectedJoysticks.Count > 0 ? ControlScheme.Joystick : ControlScheme.Keyboard;
            chosenSchemes = new[]
            {
                (ControlScheme)PlayerPrefs.GetInt("Scheme_P1", (int)ControlScheme.Keyboard),
                (ControlScheme)PlayerPrefs.GetInt("Scheme_P2", (int)p2Default)
            };
        }

        controlPanel.SetActive(true);
        RefreshControlPanel();
    }

    private void ScanJoysticks()
    {
        connectedJoysticks.Clear();
        string[] names = Input.GetJoystickNames();

        for (int i = 0; i < names.Length && i < 4; i++)
        {
            if (!string.IsNullOrEmpty(names[i]))
                connectedJoysticks.Add(i + 1);
        }
    }

    private int JoysticksNeeded()
    {
        int needed = 0;
        foreach (ControlScheme scheme in chosenSchemes)
        {
            if (scheme == ControlScheme.Joystick)
                needed++;
        }
        return needed;
    }

    private void ToggleScheme(int playerIndex)
    {
        chosenSchemes[playerIndex] = chosenSchemes[playerIndex] == ControlScheme.Keyboard
            ? ControlScheme.Joystick
            : ControlScheme.Keyboard;

        RefreshControlPanel();
    }

    private void RefreshControlPanel()
    {
        ScanJoysticks();

        if (p1DeviceText != null)
            p1DeviceText.text = chosenSchemes[0] == ControlScheme.Keyboard ? "Keyboard (WASD)" : "Joystick";

        if (p2DeviceText != null)
            p2DeviceText.text = chosenSchemes[1] == ControlScheme.Keyboard ? "Keyboard (Arrows)" : "Joystick";

        int needed = JoysticksNeeded();
        int found = connectedJoysticks.Count;
        bool enough = found >= needed;

        if (joystickStatusText != null)
        {
            string detected = found == 0 ? "No joystick detected" : $"Joysticks detected: {found}";
            joystickStatusText.text = enough ? detected : $"{detected} - connect {needed - found} more to start";
            joystickStatusText.color = enough ? Color.white : new Color(1f, 0.4f, 0.4f);
        }

        if (startBut != null)
        {
            startBut.interactable = enough;

            if (startBaseScale == null)
                startBaseScale = startBut.transform.localScale;

            startBut.transform.localScale = startBaseScale.Value * (enough ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 1f);
        }

        SetDeviceIcon(p1DeviceIcon, chosenSchemes[0]);
        SetDeviceIcon(p2DeviceIcon, chosenSchemes[1]);
    }

    private void SetDeviceIcon(Image icon, ControlScheme scheme)
    {
        if (icon != null)
            icon.sprite = scheme == ControlScheme.Keyboard ? keyboardIcon : joystickIcon;
    }

    private void StartWithChosenControls()
    {
        ScanJoysticks();
        if (connectedJoysticks.Count < JoysticksNeeded())
        {
            RefreshControlPanel();
            return;
        }

        PlayerControl[] players = FindObjectsByType<PlayerControl>(FindObjectsSortMode.None);
        System.Array.Sort(players, (a, b) => a.playerNumber.CompareTo(b.playerNumber));

        int nextJoystick = 0;
        foreach (PlayerControl player in players)
        {
            player.controlScheme = chosenSchemes[Mathf.Clamp(player.playerNumber - 1, 0, 1)];

            if (player.UsesJoystick)
                player.joystickNumber = connectedJoysticks[nextJoystick++];
        }

        PlayerPrefs.SetInt("Scheme_P1", (int)chosenSchemes[0]);
        PlayerPrefs.SetInt("Scheme_P2", (int)chosenSchemes[1]);
        PlayerPrefs.Save();

        KeybindManager.Instance?.RefreshAllKeybindDisplay();

        controlPanel.SetActive(false);
        StartMode(pendingMode);
    }

    private void BackToModeSelect()
    {
        if (controlPanel != null)
            controlPanel.SetActive(false);

        if (modePanel != null)
            modePanel.SetActive(true);
    }

    private void StartMode(OrderManager.GameMode mode)
    {
        if (modePanel != null)
            modePanel.SetActive(false);

        Time.timeScale = 1f;

        MatchRules.Instance?.Apply(mode);
        OrderManager.Instance?.SetGameMode(mode);

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ForceResume();
        }

        AudioManager.Instance?.PlayGameplayBGM();
    }

    public void ShowModeSelector()
    {
        if (retryMode.HasValue)
        {
            OrderManager.GameMode mode = retryMode.Value;
            retryMode = null;

            SelectMode(mode);
            if (controlPanel != null)
                StartWithChosenControls();
            return;
        }

        if (controlPanel != null)
            controlPanel.SetActive(false);

        if (modePanel != null)
            modePanel.SetActive(true);

        OrderUIManager.Instance?.HideStatus();
        OrderUIManager.Instance?.ClearOrderImages();
        VersusUIManager.Instance?.HideVersusUI();
    }
}

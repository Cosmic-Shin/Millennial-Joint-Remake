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

    private static readonly string[] DeviceNames = { "Keyboard (WASD)", "Keyboard (Arrows)", "Joystick 1", "Joystick 2" };
    private static int[] chosenDevices;

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

        if (p1PrevBut != null) p1PrevBut.onClick.AddListener(() => CycleDevice(0, -1));
        if (p1NextBut != null) p1NextBut.onClick.AddListener(() => CycleDevice(0, 1));
        if (p2PrevBut != null) p2PrevBut.onClick.AddListener(() => CycleDevice(1, -1));
        if (p2NextBut != null) p2NextBut.onClick.AddListener(() => CycleDevice(1, 1));

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

        if (chosenDevices == null)
        {
            chosenDevices = new[]
            {
                Mathf.Clamp(PlayerPrefs.GetInt("Device_P1", 0), 0, 3),
                Mathf.Clamp(PlayerPrefs.GetInt("Device_P2", connectedJoysticks.Count > 0 ? 2 : 1), 0, 3)
            };

            if (chosenDevices[0] == chosenDevices[1])
                chosenDevices[1] = (chosenDevices[0] + 1) % DeviceNames.Length;
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

    private int JoysticksNeeded() => Mathf.Max(chosenDevices[0], chosenDevices[1]) - 1;

    private void CycleDevice(int playerIndex, int step)
    {
        int other = chosenDevices[1 - playerIndex];
        int device = chosenDevices[playerIndex];

        do device = (device + step + DeviceNames.Length) % DeviceNames.Length;
        while (device == other);

        chosenDevices[playerIndex] = device;
        RefreshControlPanel();
    }

    private void RefreshControlPanel()
    {
        ScanJoysticks();

        if (p1DeviceText != null)
            p1DeviceText.text = DeviceNames[chosenDevices[0]];

        if (p2DeviceText != null)
            p2DeviceText.text = DeviceNames[chosenDevices[1]];

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

        SetDeviceIcon(p1DeviceIcon, chosenDevices[0]);
        SetDeviceIcon(p2DeviceIcon, chosenDevices[1]);
    }

    private void SetDeviceIcon(Image icon, int device)
    {
        if (icon != null)
            icon.sprite = device < 2 ? keyboardIcon : joystickIcon;
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

        foreach (PlayerControl player in players)
        {
            int device = chosenDevices[Mathf.Clamp(player.playerNumber - 1, 0, 1)];
            player.controlScheme = device < 2 ? ControlScheme.Keyboard : ControlScheme.Joystick;

            if (player.UsesJoystick)
                player.joystickNumber = connectedJoysticks[device - 2];
            else if (player.arrowsLayout != (device == 1) && players.Length == 2)
                PlayerControl.SwapKeyboardKeys(players[0], players[1]);
        }

        PlayerPrefs.SetInt("Device_P1", chosenDevices[0]);
        PlayerPrefs.SetInt("Device_P2", chosenDevices[1]);
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

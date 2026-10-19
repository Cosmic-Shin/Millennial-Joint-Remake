using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class KeybindManager : MonoBehaviour
{
    public static KeybindManager Instance { get; private set; }

    [Header("Player 1 Keybind Buttons")]
    [SerializeField] private Button p1MoveUpButton;
    [SerializeField] private Button p1MoveLeftButton;
    [SerializeField] private Button p1MoveDownButton;
    [SerializeField] private Button p1MoveRightButton;
    [SerializeField] private Button p1InteractButton;
    [SerializeField] private Button p1RunButton;
    [SerializeField] private Button p1ThrowButton;
    [SerializeField] private Button p1EmoteButton;

    [Header("Player 2 Keybind Buttons")]
    [SerializeField] private Button p2MoveUpButton;
    [SerializeField] private Button p2MoveLeftButton;
    [SerializeField] private Button p2MoveDownButton;
    [SerializeField] private Button p2MoveRightButton;
    [SerializeField] private Button p2InteractButton;
    [SerializeField] private Button p2RunButton;
    [SerializeField] private Button p2ThrowButton;
    [SerializeField] private Button p2EmoteButton;

    [Header("Panel Titles (shows chosen device)")]
    [SerializeField] private TextMeshProUGUI p1TitleText;
    [SerializeField] private TextMeshProUGUI p2TitleText;

    [Header("Control Buttons")]
    [SerializeField] private Button resetButton;

    [Header("Warning Message")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float warningDisplayTime = 2f;

    private static readonly string[] PadButtonNames = { "A", "B", "X", "Y", "LB", "RB", "BACK", "START", "LSB", "RSB" };

    private PlayerControl player1Control;
    private PlayerControl player2Control;

    private Button currentReBindingButton;
    private TextMeshProUGUI currentButtonText;
    private bool isListeningForKey = false;
    private int currentPlayerNumber = 0;
    private string currentKeybindType = "";
    private int listenStartFrame = -1;
    private int escapeCancelFrame = -1;

    public bool IsCapturingInput => isListeningForKey || escapeCancelFrame == Time.frameCount;

    private struct KeybindMapping
    {
        public Button button;
        public int playerNumber;
        public string fieldName;
        public string displayName;
    }

    private List<KeybindMapping> keybindMappings = new List<KeybindMapping>();

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
        FindPlayerControls();
        LoadBindings();
        SetupKeybindButtons();
        SetupResetButton();
        RefreshAllKeybindDisplay();

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    private void SetupResetButton()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetKeybindsToDefault);
    }

    private void FindPlayerControls()
    {
        PlayerControl[] allPlayers = FindObjectsByType<PlayerControl>(FindObjectsSortMode.None);

        foreach (PlayerControl player in allPlayers)
        {
            if (player.playerNumber == 1)
                player1Control = player;
            else if (player.playerNumber == 2)
                player2Control = player;
        }

        if (player1Control == null || player2Control == null)
            Debug.LogWarning("KeybindManager: Could not find both player controls!");
    }

    private PlayerControl GetPlayer(int playerNumber)
    {
        bool swapped = ControlSwapManager.Instance != null && ControlSwapManager.Instance.IsSwapped;
        return (playerNumber == 1) != swapped ? player1Control : player2Control;
    }

    private void SaveBindings()
    {
        for (int n = 1; n <= 2; n++)
        {
            PlayerControl player = GetPlayer(n);
            if (player != null)
                PlayerPrefs.SetString("Bindings_P" + n, JsonUtility.ToJson(player.Bindings));
        }

        PlayerPrefs.Save();
    }

    private void LoadBindings()
    {
        for (int n = 1; n <= 2; n++)
        {
            PlayerControl player = GetPlayer(n);
            string json = PlayerPrefs.GetString("Bindings_P" + n, "");
            if (player == null || string.IsNullOrEmpty(json))
                continue;

            PlayerControl.InputBindings saved = JsonUtility.FromJson<PlayerControl.InputBindings>(json);
            saved.scheme = player.controlScheme;
            saved.joystickNumber = player.joystickNumber;
            if (!json.Contains("\"arrows\""))
                saved.arrows = player.arrowsLayout;
            player.Bindings = saved;
        }
    }

    private void SetupKeybindButtons()
    {
        SetupButtonMapping(p1MoveUpButton, 1, "MoveUp", "Move Up");
        SetupButtonMapping(p1MoveLeftButton, 1, "MoveLeft", "Move Left");
        SetupButtonMapping(p1MoveDownButton, 1, "MoveDown", "Move Down");
        SetupButtonMapping(p1MoveRightButton, 1, "MoveRight", "Move Right");
        SetupButtonMapping(p1InteractButton, 1, "PrimaryAction", "Interact");
        SetupButtonMapping(p1RunButton, 1, "Run", "Run");
        SetupButtonMapping(p1ThrowButton, 1, "DropItem", "Throw");
        SetupButtonMapping(p1EmoteButton, 1, "EmoteSelectKey", "Emote");

        SetupButtonMapping(p2MoveUpButton, 2, "MoveUp", "Move Up");
        SetupButtonMapping(p2MoveLeftButton, 2, "MoveLeft", "Move Left");
        SetupButtonMapping(p2MoveDownButton, 2, "MoveDown", "Move Down");
        SetupButtonMapping(p2MoveRightButton, 2, "MoveRight", "Move Right");
        SetupButtonMapping(p2InteractButton, 2, "PrimaryAction", "Interact");
        SetupButtonMapping(p2RunButton, 2, "Run", "Run");
        SetupButtonMapping(p2ThrowButton, 2, "DropItem", "Throw");
        SetupButtonMapping(p2EmoteButton, 2, "EmoteSelectKey", "Emote");
    }

    private void SetupButtonMapping(Button button, int playerNumber, string fieldName, string displayName)
    {
        if (button == null)
            return;

        KeybindMapping mapping = new KeybindMapping
        {
            button = button,
            playerNumber = playerNumber,
            fieldName = fieldName,
            displayName = displayName
        };

        keybindMappings.Add(mapping);

        int capturedPlayer = playerNumber;
        string capturedFieldName = fieldName;

        button.onClick.AddListener(() =>
        {
            StartKeybindListening(capturedPlayer, capturedFieldName, button);
        });
    }

    private KeyCode GetCurrentKeyCode(int playerNumber, string fieldName)
    {
        PlayerControl player = GetPlayer(playerNumber);

        if (player == null)
            return KeyCode.None;

        if (player.UsesJoystick)
        {
            return fieldName switch
            {
                "PrimaryAction" => player.padInteractKey,
                "Run" => player.padRunKey,
                "EmoteSelectKey" => player.padEmoteKey,
                "DropItem" => player.padThrowKey,
                _ => KeyCode.None
            };
        }

        if (fieldName == "EmoteSelectKey")
            return player.EmoteKey;

        var field = typeof(PlayerControl).GetField(fieldName);
        if (field != null)
        {
            object value = field.GetValue(player);
            if (value is KeyCode keyCode)
                return keyCode;
        }

        return KeyCode.None;
    }

    private void SetKeyCode(int playerNumber, string fieldName, KeyCode keyCode)
    {
        PlayerControl player = GetPlayer(playerNumber);

        if (player == null)
            return;

        if (player.UsesJoystick)
        {
            switch (fieldName)
            {
                case "PrimaryAction": player.padInteractKey = keyCode; break;
                case "Run": player.padRunKey = keyCode; break;
                case "EmoteSelectKey": player.padEmoteKey = keyCode; break;
                case "DropItem": player.padThrowKey = keyCode; break;
            }
            return;
        }

        if (fieldName == "EmoteSelectKey")
        {
            player.EmoteKey = keyCode;
            return;
        }

        var field = typeof(PlayerControl).GetField(fieldName);
        if (field != null)
        {
            field.SetValue(player, keyCode);
        }
    }

    private bool SharesDevice(int playerA, int playerB)
    {
        if (playerA == playerB)
            return true;

        PlayerControl a = GetPlayer(playerA);
        PlayerControl b = GetPlayer(playerB);
        return a != null && b != null && !a.UsesJoystick && !b.UsesJoystick;
    }

    private bool IsKeyAlreadyBound(KeyCode key, int excludePlayerNumber, string excludeFieldName)
    {
        return GetBindingUsingKey(key, excludePlayerNumber, excludeFieldName).playerNumber != -1;
    }

    private (int playerNumber, string displayName) GetBindingUsingKey(KeyCode key, int listeningPlayer, string excludeFieldName)
    {
        foreach (var mapping in keybindMappings)
        {
            if (mapping.playerNumber == listeningPlayer && mapping.fieldName == excludeFieldName)
                continue;

            if (!SharesDevice(mapping.playerNumber, listeningPlayer))
                continue;

            KeyCode existingKey = GetCurrentKeyCode(mapping.playerNumber, mapping.fieldName);
            if (existingKey == key)
                return (mapping.playerNumber, mapping.displayName);
        }

        return (-1, "");
    }

    private void StartKeybindListening(int playerNumber, string fieldName, Button button)
    {
        if (isListeningForKey)
            return;

        isListeningForKey = true;
        listenStartFrame = Time.frameCount;
        currentPlayerNumber = playerNumber;
        currentKeybindType = fieldName;
        currentReBindingButton = button;
        currentButtonText = button.GetComponentInChildren<TextMeshProUGUI>();

        if (warningText != null)
            warningText.gameObject.SetActive(false);

        PlayerControl player = GetPlayer(playerNumber);
        bool joystick = player != null && player.UsesJoystick;

        button.GetComponent<Image>().color = new Color(0.4f, 0.6f, 1f, 1f);
        if (currentButtonText != null)
            currentButtonText.text = joystick ? "Press a button/stick..." : "Press any key...";
    }

    private void Update()
    {
        if (!isListeningForKey || Time.frameCount == listenStartFrame)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapeCancelFrame = Time.frameCount;
            CancelKeybindListening();
            return;
        }

        PlayerControl player = GetPlayer(currentPlayerNumber);
        if (player == null)
        {
            CancelKeybindListening();
            return;
        }

        if (player.UsesJoystick && currentKeybindType.StartsWith("Move"))
        {
            foreach (ControllerAxisSource source in System.Enum.GetValues(typeof(ControllerAxisSource)))
            {
                float h = Input.GetAxisRaw(PlayerControl.AxisName(player.joystickNumber, source, true));
                float v = Input.GetAxisRaw(PlayerControl.AxisName(player.joystickNumber, source, false));

                if (Mathf.Abs(h) > 0.5f || Mathf.Abs(v) > 0.5f)
                {
                    player.padMovementAxis = source;
                    FinishListening();
                    return;
                }
            }

            return;
        }

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.None || !Input.GetKeyDown(key))
                continue;

            bool isPadKey = key >= KeyCode.JoystickButton0;
            if (isPadKey != player.UsesJoystick)
                continue;

            KeyCode bindKey = isPadKey ? KeyCode.Joystick1Button0 + (key - KeyCode.JoystickButton0) % 20 : key;

            if (IsKeyAlreadyBound(bindKey, currentPlayerNumber, currentKeybindType))
            {
                var (boundPlayer, boundAction) = GetBindingUsingKey(bindKey, currentPlayerNumber, currentKeybindType);
                ShowWarning($"Key '{FormatKey(player, bindKey)}' is already used by Player {boundPlayer}: {boundAction}");
                CancelKeybindListening();
                return;
            }

            ApplyNewKeybind(bindKey);
            return;
        }
    }

    private void FinishListening()
    {
        if (currentReBindingButton != null)
            currentReBindingButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        isListeningForKey = false;
        SaveBindings();
        RefreshAllKeybindDisplay();
    }

    private void ApplyNewKeybind(KeyCode key)
    {
        SetKeyCode(currentPlayerNumber, currentKeybindType, key);
        FinishListening();
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning(message);

        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideWarning));
            Invoke(nameof(HideWarning), warningDisplayTime);
        }
    }

    private void HideWarning()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    private void CancelKeybindListening()
    {
        if (currentReBindingButton != null)
        {
            currentReBindingButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        isListeningForKey = false;
        RefreshAllKeybindDisplay();
    }

    public void RefreshAllKeybindDisplay()
    {
        foreach (var mapping in keybindMappings)
        {
            if (mapping.button == null) continue;

            TextMeshProUGUI buttonText = mapping.button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = GetFormattedDisplayString(mapping.playerNumber, mapping.fieldName);
            }
        }

        RefreshTitle(p1TitleText, 1);
        RefreshTitle(p2TitleText, 2);
    }

    private void RefreshTitle(TextMeshProUGUI title, int playerNumber)
    {
        PlayerControl player = GetPlayer(playerNumber);
        if (title == null || player == null)
            return;

        title.text = $"Player {playerNumber} - {(player.UsesJoystick ? "Joystick" : player.arrowsLayout ? "Arrows" : "WASD")}";
    }

    private void ResetKeybindsToDefault()
    {
        ControlSwapManager.Instance?.RestoreOriginalControls();

        if (player1Control != null) player1Control.ResetBindingsToDefault();
        if (player2Control != null) player2Control.ResetBindingsToDefault();
        SaveBindings();
        RefreshAllKeybindDisplay();
    }

    private string FormatKey(PlayerControl player, KeyCode code)
    {
        if (player.UsesJoystick && code >= KeyCode.JoystickButton0)
        {
            int index = (code - KeyCode.JoystickButton0) % 20;
            return index < PadButtonNames.Length ? PadButtonNames[index] : "BUTTON" + index;
        }

        return code.ToString().ToUpper();
    }

    private string GetFormattedDisplayString(int playerNumber, string fieldName)
    {
        PlayerControl player = GetPlayer(playerNumber);
        if (player == null) return "";

        if (player.UsesJoystick && fieldName.StartsWith("Move"))
        {
            string stickPrefix = player.padMovementAxis switch
            {
                ControllerAxisSource.RSB => "RSB",
                ControllerAxisSource.DPad => "DPAD_",
                _ => "LSB"
            };

            return fieldName switch
            {
                "MoveUp" => $"{stickPrefix}UP",
                "MoveLeft" => $"{stickPrefix}LEFT",
                "MoveDown" => $"{stickPrefix}DOWN",
                "MoveRight" => $"{stickPrefix}RIGHT",
                _ => ""
            };
        }

        return FormatKey(player, GetCurrentKeyCode(playerNumber, fieldName));
    }
}

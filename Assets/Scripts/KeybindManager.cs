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

    [Header("Control Buttons")]
    [SerializeField] private Button resetButton;

    [Header("Warning Message")]
    [SerializeField] private TextMeshProUGUI warningText; // Optional: shows "Key already in use!"
    [SerializeField] private float warningDisplayTime = 2f;

    private PlayerControl player1Control;
    private PlayerControl player2Control;

    private Button currentReBindingButton;
    private TextMeshProUGUI currentButtonText;
    private bool isListeningForKey = false;
    private int currentPlayerNumber = 0;
    private string currentKeybindType = "";

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
        SetupKeybindButtons();
        SetupResetButton();
        RefreshAllKeybindDisplay();

        // Hide warning initially
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
        PlayerControl[] allPlayers = FindObjectsOfType<PlayerControl>();

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

    private void SetupKeybindButtons()
    {
        // Player 1
        SetupButtonMapping(p1MoveUpButton, 1, "MoveUp", "Move Up");
        SetupButtonMapping(p1MoveLeftButton, 1, "MoveLeft", "Move Left");
        SetupButtonMapping(p1MoveDownButton, 1, "MoveDown", "Move Down");
        SetupButtonMapping(p1MoveRightButton, 1, "MoveRight", "Move Right");
        SetupButtonMapping(p1InteractButton, 1, "PrimaryAction", "Interact");
        SetupButtonMapping(p1RunButton, 1, "Run", "Run");
        SetupButtonMapping(p1ThrowButton, 1, "DropItem", "Throw");
        SetupButtonMapping(p1EmoteButton, 1, "EmoteSelectKey", "Emote");

        // Player 2
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
        PlayerControl player = playerNumber == 1 ? player1Control : player2Control;

        if (player == null)
            return KeyCode.None;

        // Handle emote key separately for each player
        if (fieldName == "EmoteSelectKey")
        {
            return playerNumber == 1 ? player.p1SelectKey : player.p2SelectKey;
        }

        // Use reflection to get the field
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
        PlayerControl player = playerNumber == 1 ? player1Control : player2Control;

        if (player == null)
            return;

        // Handle emote key separately for each player
        if (fieldName == "EmoteSelectKey")
        {
            if (playerNumber == 1)
                player.p1SelectKey = keyCode;
            else
                player.p2SelectKey = keyCode;
            return;
        }

        // Use reflection to set the field
        var field = typeof(PlayerControl).GetField(fieldName);
        if (field != null)
        {
            field.SetValue(player, keyCode);
        }
    }

    /// <summary>
    /// Checks if a key is already used by ANY keybind
    /// Returns true if the key is a duplicate
    /// </summary>
    private bool IsKeyAlreadyBound(KeyCode key, int excludePlayerNumber, string excludeFieldName)
    {
        foreach (var mapping in keybindMappings)
        {
            // Skip the current binding being changed
            if (mapping.playerNumber == excludePlayerNumber && mapping.fieldName == excludeFieldName)
                continue;

            KeyCode existingKey = GetCurrentKeyCode(mapping.playerNumber, mapping.fieldName);
            if (existingKey == key)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds which binding is using a specific key
    /// </summary>
    private (int playerNumber, string displayName) GetBindingUsingKey(KeyCode key)
    {
        foreach (var mapping in keybindMappings)
        {
            KeyCode existingKey = GetCurrentKeyCode(mapping.playerNumber, mapping.fieldName);
            if (existingKey == key)
                return (mapping.playerNumber, mapping.displayName);
        }

        return (-1, "");
    }

    /// <summary>
    /// Clears a specific key from the player who currently has it
    /// </summary>
    private void ClearKeyFromOtherBinding(KeyCode key, int excludePlayerNumber, string excludeFieldName)
    {
        foreach (var mapping in keybindMappings)
        {
            if (mapping.playerNumber == excludePlayerNumber && mapping.fieldName == excludeFieldName)
                continue;

            KeyCode existingKey = GetCurrentKeyCode(mapping.playerNumber, mapping.fieldName);
            if (existingKey == key)
            {
                // Set to None to clear it
                SetKeyCode(mapping.playerNumber, mapping.fieldName, KeyCode.None);
                
                // Update the button text
                TextMeshProUGUI btnText = mapping.button.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                    btnText.text = "None";
                
                break;
            }
        }
    }

    private void StartKeybindListening(int playerNumber, string fieldName, Button button)
    {
        if (isListeningForKey)
            return;

        isListeningForKey = true;
        currentPlayerNumber = playerNumber;
        currentKeybindType = fieldName;
        currentReBindingButton = button;
        currentButtonText = button.GetComponentInChildren<TextMeshProUGUI>();

        // Hide warning
        if (warningText != null)
            warningText.gameObject.SetActive(false);

        // Visual feedback
        button.GetComponent<Image>().color = new Color(0.4f, 0.6f, 1f, 1f);
        if (currentButtonText != null)
            currentButtonText.text = "Press any key...";

        Debug.Log($"Listening for keybind input for Player {playerNumber}: {fieldName}");
    }

    private void Update()
    {
        if (!isListeningForKey)
            return;

        // Listen for any key press
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key))
            {
                // ESC cancels the rebinding
                if (key == KeyCode.Escape)
                {
                    CancelKeybindListening();
                    return;
                }

                // Ignore None key
                if (key == KeyCode.None)
                    continue;

                // Check if this key is already used by another binding
                if (IsKeyAlreadyBound(key, currentPlayerNumber, currentKeybindType))
                {
                    // Show warning
                    var (boundPlayer, boundAction) = GetBindingUsingKey(key);
                    ShowWarning($"Key '{key}' is already used by Player {boundPlayer}: {boundAction}");

                    // Option 1: Just cancel and don't allow duplicate
                    CancelKeybindListening();
                    
                    // Option 2: Uncomment below to STEAL the key (clear old binding and assign new one)
                    // ClearKeyFromOtherBinding(key, currentPlayerNumber, currentKeybindType);
                    // ApplyNewKeybind(key);
                    
                    return;
                }

                // Key is free, apply it
                ApplyNewKeybind(key);
                return;
            }
        }
    }

    private void ApplyNewKeybind(KeyCode key)
    {
        SetKeyCode(currentPlayerNumber, currentKeybindType, key);

        // Find the display name for this keybind
        string displayName = currentKeybindType;
        foreach (var mapping in keybindMappings)
        {
            if (mapping.button == currentReBindingButton)
            {
                displayName = mapping.displayName;
                break;
            }
        }

        // Update UI with new keybind
        if (currentButtonText != null)
            currentButtonText.text = $"{key}";

        // Reset visual feedback
        if (currentReBindingButton != null)
            currentReBindingButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

        isListeningForKey = false;
        Debug.Log($"Keybind set: {displayName} (P{currentPlayerNumber}) = {key}");
    }

    private void ShowWarning(string message)
    {
        Debug.LogWarning(message);

        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            
            // Auto-hide after delay
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
            
            // Restore the current key text
            foreach (var mapping in keybindMappings)
            {
                if (mapping.button == currentReBindingButton)
                {
                    KeyCode currentKey = GetCurrentKeyCode(mapping.playerNumber, mapping.fieldName);
                    if (currentButtonText != null)
                        currentButtonText.text = $"{currentKey}";
                    break;
                }
            }
        }

        isListeningForKey = false;
        Debug.Log("Keybind listening cancelled");
    }

    public void RefreshAllKeybindDisplay()
    {
        foreach (var mapping in keybindMappings)
        {
            if (mapping.button == null)
                continue;

            TextMeshProUGUI buttonText = mapping.button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                KeyCode currentKey = GetCurrentKeyCode(mapping.playerNumber, mapping.fieldName);
                buttonText.text = $"{currentKey}";
            }
        }
    }

    private void ResetKeybindsToDefault()
    {
        // Reset Player 1 & 2 to default keybinds
        ResetPlayerKeybinds(1);
        ResetPlayerKeybinds(2);
        RefreshAllKeybindDisplay();
        Debug.Log("Keybinds reset to default");
    }

    private void ResetPlayerKeybinds(int playerNumber)
    {
        if (playerNumber == 1)
        {
            // Player 1 defaults
            SetKeyCode(playerNumber, "MoveUp", KeyCode.W);
            SetKeyCode(playerNumber, "MoveLeft", KeyCode.A);
            SetKeyCode(playerNumber, "MoveDown", KeyCode.S);
            SetKeyCode(playerNumber, "MoveRight", KeyCode.D);
            SetKeyCode(playerNumber, "PrimaryAction", KeyCode.F);
            SetKeyCode(playerNumber, "Run", KeyCode.LeftShift);
            SetKeyCode(playerNumber, "DropItem", KeyCode.Q);
            SetKeyCode(playerNumber, "EmoteSelectKey", KeyCode.Z);
        }
        else
        {
            // Player 2 defaults
            SetKeyCode(playerNumber, "MoveUp", KeyCode.UpArrow);
            SetKeyCode(playerNumber, "MoveLeft", KeyCode.LeftArrow);
            SetKeyCode(playerNumber, "MoveDown", KeyCode.DownArrow);
            SetKeyCode(playerNumber, "MoveRight", KeyCode.RightArrow);
            SetKeyCode(playerNumber, "PrimaryAction", KeyCode.Return);
            SetKeyCode(playerNumber, "Run", KeyCode.LeftShift);
            SetKeyCode(playerNumber, "DropItem", KeyCode.RightControl);
            SetKeyCode(playerNumber, "EmoteSelectKey", KeyCode.Backspace);
        }
    }
}
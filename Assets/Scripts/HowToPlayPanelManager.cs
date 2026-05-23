using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanelManager : MonoBehaviour
{
    [Header("Player Panels")]
    [SerializeField] private GameObject player1Panel;
    [SerializeField] private GameObject player2Panel;

    [Header("Navigation Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private int currentPlayerIndex = 0; // 0 = Player 1, 1 = Player 2

    private void Start()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(ShowPreviousPlayer);

        if (rightButton != null)
            rightButton.onClick.AddListener(ShowNextPlayer);

        // Initialize with Player 1 panel visible
        ShowPlayer(0);
    }

    private void ShowPreviousPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex - 1 + 2) % 2;
        ShowPlayer(currentPlayerIndex);
    }

    private void ShowNextPlayer()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % 2;
        ShowPlayer(currentPlayerIndex);
    }

    private void ShowPlayer(int playerIndex)
    {
        currentPlayerIndex = playerIndex;

        // Show/hide panels
        if (player1Panel != null)
            player1Panel.SetActive(playerIndex == 0);

        if (player2Panel != null)
            player2Panel.SetActive(playerIndex == 1);

        // Update button states
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        // Both buttons are always interactable since they cycle
        if (leftButton != null)
            leftButton.interactable = true;

        if (rightButton != null)
            rightButton.interactable = true;
    }
}

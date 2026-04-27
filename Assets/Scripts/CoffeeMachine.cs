using UnityEngine;

/// <summary>
/// Coffee machine gives coffee after a pouring timer.
/// Player cannot move while pouring.
/// </summary>
public class CoffeeMachine : BaseStation, IInteractable
{
    [Header("Pouring")]
    public float pouringTime = 3f; // Adjustable pouring time
    public GameObject pouringUIPanel; // UI panel to show during pouring

    private bool isPouring = false;
    private float pouringTimer = 0f;
    private PlayerControl currentPlayer;
    private void Update()
    {
        if (isPouring)
        {
            pouringTimer -= Time.deltaTime;
            if (pouringTimer <= 0f)
            {
                // Pouring finished
                if (currentPlayer != null)
                {
                    currentPlayer.heldItem.Set(ItemType.Cup);
                    currentPlayer.heldItem.cupHasCoffee = true;
                    currentPlayer.RefreshHeldItemDisplay();
                    currentPlayer.doMove = true;
                    Show(currentPlayer, "Grabbed coffee!");
                }
                ShowPouringUI(false);
                isPouring = false;
                currentPlayer = null;
            }
        }
    }

    private void ShowPouringUI(bool show)
    {
        if (pouringUIPanel != null)
            pouringUIPanel.SetActive(show);
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty && !isPouring;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (isPouring)
        {
            Show(player, $"Pouring coffee... {pouringTimer:F1}s left");
            return;
        }

        if (!player.heldItem.IsEmpty)
        {
            Show(player, "Hands must be empty to grab coffee");
            return;
        }

        // Start pouring
        currentPlayer = player;
        currentPlayer.doMove = false;
        isPouring = true;
        pouringTimer = pouringTime;
        ShowPouringUI(true);
        Show(player, "Pouring coffee...");
    }
}
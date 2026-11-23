using UnityEngine;
using UnityEngine.UI;

public class CoffeeMachine : BaseStation, IInteractable
{
    [Header("Pouring")]
    public float pouringTime = 3f;
    public GameObject pouringUIPanel;

    [Tooltip("Anchor for the timer ring (its own sprite stays hidden)")]
    public Image pouringUIImage;

    private bool isPouring = false;
    private float pouringTimer = 0f;
    private PlayerControl currentPlayer;

    private void Update()
    {
        if (isPouring)
        {
            pouringTimer -= Time.deltaTime * CookRate(currentPlayer);
            SetCookRing(pouringUIImage, 1f - pouringTimer / pouringTime, new Color(0.85f, 0.6f, 0.35f));

            if (pouringTimer <= 0f)
            {
                if (currentPlayer != null)
                {
                    currentPlayer.currentCoffeeMachine = null;

                    currentPlayer.heldItem.Set(ItemType.Cup);
                    currentPlayer.heldItem.cupHasCoffee = true;
                    currentPlayer.RefreshHeldItemDisplay();
                    currentPlayer.doMove = true;
                    Show(currentPlayer, "Grabbed coffee!");
                    AudioManager.Instance?.PlayFinishedDrinkCoffeeSFX();
                    AudioManager.Instance?.PlayGetPlateAndCupSFX();
                }

                ShowPouringUI(false);
                isPouring = false;
                currentPlayer = null;
            }
        }
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

        if (!StoryMode.OnMenu(OrderItemType.Coffee))
        {
            Show(player, "Not on today's menu");
            player.PlayNope();
            return;
        }

        currentPlayer = player;
        currentPlayer.currentCoffeeMachine = this;
        currentPlayer.doMove = false;

        isPouring = true;

        pouringTimer = pouringTime;

        ShowPouringUI(true);
        Show(player, "Pouring coffee...");
        AudioManager.Instance?.PlayStartDrinkCoffeeSFX();
    }

    private void ShowPouringUI(bool show)
    {
        if (pouringUIPanel != null)
            pouringUIPanel.SetActive(show);
    }
}
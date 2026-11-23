using UnityEngine;
using UnityEngine.UI;

public class DrinkMachine : BaseStation, IInteractable
{
    [Header("Drink Selection")]
    private string[] drinkSelectionLabels = new string[]
    {
        "Soda",
        "Ice Tea",
        "Orange Juice"
    };

    private static readonly OrderItemType[] drinkOrders = { OrderItemType.Soda, OrderItemType.IceTea, OrderItemType.OrangeJuice };

    [Header("Smart Suggestion")]
    public Color suggestionTextColor = new Color(0.05f, 0.5f, 0.1f);
    private int suggestedDrinkIndex = -1;
    private Color normalTextColor = Color.black;

    [Header("Pouring")]
    public float pouringTime = 3f;
    public GameObject pouringUIPanel;

    [Tooltip("Anchor for the timer ring (its own sprite stays hidden)")]
    public Image pouringUIImage;

    private PlayerControl currentPlayer;
    private bool isSelectingDrink;
    private bool isPouring;
    private int selectedDrinkIndex;

    private float pouringTimer;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (currentPlayer != null && currentPlayer != player)
            return false;

        if ((isSelectingDrink || isPouring) && currentPlayer == player)
            return true;

        return player.heldItem.IsCup && !player.heldItem.IsCompleteDrink;
    }

    private static bool AnyOnMenu => System.Array.Exists(drinkOrders, StoryMode.OnMenu);

    private void Update()
    {
        if (!isPouring)
            return;

        pouringTimer -= Time.deltaTime * CookRate(currentPlayer);
        SetCookRing(pouringUIImage, 1f - pouringTimer / pouringTime, new Color(0.4f, 0.8f, 1f));

        if (pouringTimer <= 0f)
        {
            FinishPouring();
        }
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        if (currentPlayer != null && currentPlayer != player) return;

        if (isPouring)
        {
            Show(player, $"Pouring drink... {pouringTimer:F1}s left");
            return;
        }

        if (!isSelectingDrink)
        {
            if (!player.heldItem.IsCup)
            {
                Show(player, "Hold an empty cup first");
                return;
            }

            if (player.heldItem.IsCompleteDrink)
            {
                Show(player, "Cup is already filled");
                return;
            }

            if (!AnyOnMenu)
            {
                Show(player, "Not on today's menu");
                player.PlayNope();
                return;
            }

            currentPlayer = player;
            currentPlayer.currentDrinkMachine = this;
            StartDrinkSelection();
        }
        else
        {
            StartPouring();
        }
    }

    private void StartDrinkSelection()
    {
        isSelectingDrink = true;

        QueuedOrder urgent = OrderManager.Instance != null
            ? OrderManager.Instance.MostUrgentOrder(currentPlayer, dish => System.Array.IndexOf(drinkOrders, dish) >= 0)
            : null;
        suggestedDrinkIndex = urgent != null ? System.Array.IndexOf(drinkOrders, urgent.item.type) : -1;
        selectedDrinkIndex = suggestedDrinkIndex >= 0 ? suggestedDrinkIndex : StoryMode.NextOnMenu(drinkOrders, -1, 1);

        if (currentPlayer.emoteSelectionText != null)
            normalTextColor = currentPlayer.emoteSelectionText.color;

        currentPlayer.doMove = false;
        currentPlayer.ResetMenuStep();

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);

        UpdateDrinkSelectionText();
    }

    private void StartPouring()
    {
        isSelectingDrink = false;
        isPouring = true;

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(false);

        if (currentPlayer.emoteSelectionText != null)
            currentPlayer.emoteSelectionText.text = string.Empty;

        pouringTimer = pouringTime;

        ShowPouringUI(true);
        Show(currentPlayer, "Pouring drink...");
        AudioManager.Instance?.PlayStartDrinkCoffeeSFX();
    }

    private void FinishPouring()
    {
        if (currentPlayer != null)
        {
            currentPlayer.heldItem.cupHasSoda = false;
            currentPlayer.heldItem.cupHasIceTea = false;
            currentPlayer.heldItem.cupHasOrangeJuice = false;

            switch (selectedDrinkIndex)
            {
                case 0:
                    currentPlayer.heldItem.cupHasSoda = true;
                    Show(currentPlayer, "Filled cup with Soda");
                    break;

                case 1:
                    currentPlayer.heldItem.cupHasIceTea = true;
                    Show(currentPlayer, "Filled cup with Ice Tea");
                    break;

                case 2:
                    currentPlayer.heldItem.cupHasOrangeJuice = true;
                    Show(currentPlayer, "Filled cup with Orange Juice");
                    break;
            }

            currentPlayer.RefreshHeldItemDisplay();
            AudioManager.Instance?.PlayFinishedDrinkCoffeeSFX();
            AudioManager.Instance?.PlayGetPlateAndCupSFX();
        }

        EndDrinkMachineUse();
    }

    private void EndDrinkMachineUse()
    {
        isSelectingDrink = false;
        isPouring = false;

        ShowPouringUI(false);

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentDrinkMachine = null;

            if (currentPlayer.emoteSelectionObject != null)
                currentPlayer.emoteSelectionObject.SetActive(false);

            if (currentPlayer.emoteSelectionText != null)
            {
                currentPlayer.emoteSelectionText.text = string.Empty;
                currentPlayer.emoteSelectionText.color = normalTextColor;
            }
        }

        currentPlayer = null;
    }

    private void UpdateDrinkSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        currentPlayer.emoteSelectionText.text = drinkSelectionLabels[selectedDrinkIndex];
        currentPlayer.emoteSelectionText.color = selectedDrinkIndex == suggestedDrinkIndex ? suggestionTextColor : normalTextColor;
    }

    public void HandleDrinkSelectionInput()
    {
        if (!isSelectingDrink || currentPlayer == null)
            return;

        if (currentPlayer.IsDropItemPressed() ||
            Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndDrinkMachineUse();
            return;
        }

        int step = currentPlayer.GetMenuStep();
        if (step != 0)
        {
            selectedDrinkIndex = StoryMode.NextOnMenu(drinkOrders, selectedDrinkIndex, step);
            UpdateDrinkSelectionText();
        }
    }

    private void ShowPouringUI(bool show)
    {
        if (pouringUIPanel != null)
            pouringUIPanel.SetActive(show);
    }

    private void LateUpdate()
    {
        if (!isSelectingDrink || currentPlayer == null)
            return;

        if (currentPlayer.emoteSelectionObject == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform textTransform = currentPlayer.emoteSelectionObject.transform;

        Vector3 direction = cam.transform.position - textTransform.position;
        textTransform.rotation = Quaternion.LookRotation(direction);
        textTransform.Rotate(0f, 180f, 0f);
    }
}
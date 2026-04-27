using UnityEngine;

public class DrinkMachine : BaseStation, IInteractable
{
    [Header("Drink Selection")]
    private string[] drinkSelectionLabels = new string[]
    {
        "Soda",
        "Ice Tea",
        "Orange Juice"
    };

    private PlayerControl currentPlayer;
    private bool isSelectingDrink;
    private int selectedDrinkIndex;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        // Allow same player to press F again to confirm
        if (isSelectingDrink && currentPlayer == player)
            return true;

        return player.heldItem.IsCup
               && !player.heldItem.cupHasSoda
               && !player.heldItem.cupHasIceTea
               && !player.heldItem.cupHasOrangeJuice;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!isSelectingDrink)
        {
            if (!player.heldItem.IsCup)
            {
                Show(player, "Hold an empty cup first");
                return;
            }

            if (player.heldItem.cupHasSoda || player.heldItem.cupHasIceTea || player.heldItem.cupHasOrangeJuice)
            {
                Show(player, "Cup is already filled");
                return;
            }

            currentPlayer = player;
            currentPlayer.currentDrinkMachine = this;
            StartDrinkSelection();
        }
        else
        {
            ConfirmDrinkSelection();
        }
    }

    private void StartDrinkSelection()
    {
        if (drinkSelectionLabels == null || drinkSelectionLabels.Length == 0)
            return;

        isSelectingDrink = true;
        selectedDrinkIndex = 0;

        currentPlayer.doMove = false;

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);

        UpdateDrinkSelectionText();
    }

    private void ConfirmDrinkSelection()
    {
        if (currentPlayer == null)
            return;

        switch (selectedDrinkIndex)
        {
            case 0:
                currentPlayer.heldItem.cupHasSoda = true;
                currentPlayer.heldItem.cupHasIceTea = false;
                currentPlayer.heldItem.cupHasOrangeJuice = false;
                Show(currentPlayer, "Filled cup with Soda");
                break;

            case 1:
                currentPlayer.heldItem.cupHasIceTea = true;
                currentPlayer.heldItem.cupHasSoda = false;
                currentPlayer.heldItem.cupHasOrangeJuice = false;
                Show(currentPlayer, "Filled cup with Ice Tea");
                break;

            case 2:
                currentPlayer.heldItem.cupHasOrangeJuice = true;
                currentPlayer.heldItem.cupHasSoda = false;
                currentPlayer.heldItem.cupHasIceTea = false;
                Show(currentPlayer, "Filled cup with Orange Juice");
                break;
        }

        currentPlayer.RefreshHeldItemDisplay();
        EndDrinkSelection();
    }

    private void EndDrinkSelection()
    {
        isSelectingDrink = false;

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentDrinkMachine = null;

            if (currentPlayer.emoteSelectionObject != null)
                currentPlayer.emoteSelectionObject.SetActive(false);

            if (currentPlayer.emoteSelectionText != null)
                currentPlayer.emoteSelectionText.text = string.Empty;
        }

        currentPlayer = null;
    }

    private void UpdateDrinkSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        if (drinkSelectionLabels == null || selectedDrinkIndex >= drinkSelectionLabels.Length)
            return;

        currentPlayer.emoteSelectionText.text = drinkSelectionLabels[selectedDrinkIndex];
    }

    public void HandleDrinkSelectionInput()
    {
        if (!isSelectingDrink || currentPlayer == null)
            return;

        // Cancel selection if player moves away
        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndDrinkSelection();
            return;
        }

        if (Input.GetKeyDown(currentPlayer.MoveLeft))
        {
            selectedDrinkIndex = (selectedDrinkIndex - 1 + drinkSelectionLabels.Length) % drinkSelectionLabels.Length;
            UpdateDrinkSelectionText();
        }
        else if (Input.GetKeyDown(currentPlayer.MoveRight))
        {
            selectedDrinkIndex = (selectedDrinkIndex + 1) % drinkSelectionLabels.Length;
            UpdateDrinkSelectionText();
        }
    }
}
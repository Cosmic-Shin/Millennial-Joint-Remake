using UnityEngine;
using UnityEngine.UI;

public class IngredientBox : BaseStation, IInteractable
{
    [Header("Ingredient Selection")]
    public ItemType[] selectableIngredients = new ItemType[]
    {
        ItemType.Bun,
        ItemType.Bread,
        ItemType.VeggieRaw,
        ItemType.PattyRaw,
        ItemType.FrozenFries,
        ItemType.ChickenRaw,
        ItemType.HamRaw
    };

    private PlayerControl currentPlayer;
    private bool isSelectingIngredient;
    private int selectedIngredientIndex;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        currentPlayer = player;

        if (!isSelectingIngredient)
        {
            StartIngredientSelection();
        }
        else
        {
            ConfirmIngredientSelection();
        }
    }

    private void StartIngredientSelection()
    {
        if (selectableIngredients == null || selectableIngredients.Length == 0)
            return;

        isSelectingIngredient = true;
        selectedIngredientIndex = 0;
        currentPlayer.doMove = false;
        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);
        UpdateIngredientSelectionText();
    }

    private void ConfirmIngredientSelection()
    {
        if (selectableIngredients == null || selectedIngredientIndex >= selectableIngredients.Length)
            return;

        ItemType selectedType = selectableIngredients[selectedIngredientIndex];
        currentPlayer.heldItem.Set(selectedType);
        currentPlayer.RefreshHeldItemDisplay();
        Show(currentPlayer, "Picked up " + currentPlayer.heldItem.GetDisplayName());

        EndIngredientSelection();
    }

    private void EndIngredientSelection()
    {
        isSelectingIngredient = false;
        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentIngredientBox = null;
            if (currentPlayer.emoteSelectionObject != null)
                currentPlayer.emoteSelectionObject.SetActive(false);
            currentPlayer.emoteSelectionText.text = string.Empty;
        }
        currentPlayer = null;
    }

    private void UpdateIngredientSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        if (selectableIngredients == null || selectedIngredientIndex >= selectableIngredients.Length)
            return;

        ItemType selectedType = selectableIngredients[selectedIngredientIndex];
        string displayName = GetIngredientDisplayName(selectedType);
        currentPlayer.emoteSelectionText.text = displayName;
    }

    private string GetIngredientDisplayName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Bun: return "Bun";
            case ItemType.Bread: return "Bread";
            case ItemType.VeggieRaw: return "Veggie";
            case ItemType.PattyRaw: return "Patty";
            case ItemType.FrozenFries: return "Frozen Fries";
            case ItemType.ChickenRaw: return "Chicken";
            case ItemType.HamRaw: return "Ham";
            default: return type.ToString();
        }
    }

    public void HandleIngredientSelectionInput()
    {
        if (!isSelectingIngredient || currentPlayer == null)
            return;

        // Cancel selection if player moves away
        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndIngredientSelection();
            return;
        }

        if (Input.GetKeyDown(currentPlayer.MoveLeft))
        {
            selectedIngredientIndex = (selectedIngredientIndex - 1 + selectableIngredients.Length) % selectableIngredients.Length;
            UpdateIngredientSelectionText();
        }
        else if (Input.GetKeyDown(currentPlayer.MoveRight))
        {
            selectedIngredientIndex = (selectedIngredientIndex + 1) % selectableIngredients.Length;
            UpdateIngredientSelectionText();
        }
    }
}
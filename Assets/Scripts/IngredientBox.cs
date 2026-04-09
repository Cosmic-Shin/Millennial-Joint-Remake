using UnityEngine;

public class IngredientBox : BaseStation, IInteractable
{
    public IngredientSelectionPanel selectionPanel;

    // Can only grab from a box when hands are empty and not holding a complete drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        if (player.heldItem.IsCompleteDrink) return false;
        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (selectionPanel == null)
        {
            Show(player, "Ingredient selection panel not configured");
            return;
        }

        selectionPanel.ShowPanel(player, this);
    }
}
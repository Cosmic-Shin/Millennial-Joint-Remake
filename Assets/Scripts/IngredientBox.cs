using UnityEngine;

public class IngredientBox : BaseStation, IInteractable
{
    public IngredientSelectionPanel selectionPanel;

    // Can only grab from a box when hands are empty
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
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
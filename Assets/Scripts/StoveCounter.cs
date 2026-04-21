using UnityEngine;

/// <summary>
/// Stove now only cooks PattyRaw → PattyCooked.
/// Ham no longer needs cooking (goes directly to sandwich from IngredientBox).
/// Chicken is handled separately by a FryerCounter if used.
/// </summary>
public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // Valid interactions:
    //   - Stove empty + holding PattyRaw → place it
    //   - Stove has PattyRaw + empty hands → cook it
    //   - Stove has PattyCooked + empty hands → pick it up
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.PattyRaw;

        if (storedItem.type == ItemType.PattyRaw)
            return player.heldItem.IsEmpty; // tap to cook

        if (storedItem.type == ItemType.PattyCooked)
            return player.heldItem.IsEmpty; // pick up

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        // Place raw patty
        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.PattyRaw)
            {
                storedItem.Set(ItemType.PattyRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed raw patty on stove");
                return;
            }

            Show(player, "Place a raw patty on the stove");
            return;
        }

        // Cook the patty
        if (storedItem.type == ItemType.PattyRaw)
        {
            storedItem.Set(ItemType.PattyCooked);
            UpdateStoredItemVisual();
            Show(player, "Patty cooked!");
            return;
        }

        // Pick up cooked patty
        if (storedItem.type == ItemType.PattyCooked)
        {
            player.heldItem.Set(ItemType.PattyCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Picked up cooked patty");
            return;
        }

        Show(player, "Cannot use stove right now");
    }

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}
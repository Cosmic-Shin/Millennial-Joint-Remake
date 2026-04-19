using UnityEngine;

public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // Valid interactions:
    //   - Stove empty + holding PattyRaw or HamRaw → place it
    //   - Stove has PattyRaw or HamRaw + empty hands → cook it
    //   - Stove has PattyCooked or HamCooked + empty hands → pick it up
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.PattyRaw || player.heldItem.type == ItemType.HamRaw;

        if (storedItem.type == ItemType.PattyRaw || storedItem.type == ItemType.HamRaw)
            return player.heldItem.IsEmpty; // tap to cook

        if (storedItem.type == ItemType.PattyCooked || storedItem.type == ItemType.HamCooked)
            return player.heldItem.IsEmpty; // pick up

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.HamRaw)
            {
                storedItem.Set(ItemType.HamRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed raw ham on stove");
                return;
            }

            if (player.heldItem.type == ItemType.PattyRaw)
            {
                storedItem.Set(ItemType.PattyRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed raw patty on stove");
                return;
            }

            Show(player, "Place raw ham or raw patty on stove");
            return;
        }

        if (storedItem.type == ItemType.PattyRaw)
        {
            storedItem.Set(ItemType.PattyCooked);
            UpdateStoredItemVisual();
            Show(player, "Patty cooked");
            return;
        }

        if (storedItem.type == ItemType.HamRaw)
        {
            storedItem.Set(ItemType.HamCooked);
            UpdateStoredItemVisual();
            Show(player, "Ham cooked");
            return;
        }

        if (storedItem.type == ItemType.PattyCooked)
        {
            player.heldItem.Set(ItemType.PattyCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Picked up cooked patty");
            return;
        }

        if (storedItem.type == ItemType.HamCooked)
        {
            player.heldItem.Set(ItemType.HamCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Picked up cooked ham");
            return;
        }

        Show(player, "Cannot use stove now");
    }

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}
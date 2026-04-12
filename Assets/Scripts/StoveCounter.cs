using UnityEngine;

public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // Valid interactions:
    //   - Stove empty + holding PattyRaw, BaconRaw, or ChickenRaw → place it
    //   - Stove has PattyRaw, BaconRaw, or ChickenRaw + empty hands → cook it
    //   - Stove has PattyCooked, BaconCooked, or ChickenCooked + empty hands → pick it up
    //   - Cannot interact if holding a complete drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        if (player.heldItem.IsCompleteDrink) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.PattyRaw || player.heldItem.type == ItemType.BaconRaw || player.heldItem.type == ItemType.ChickenRaw;

        if (storedItem.type == ItemType.PattyRaw || storedItem.type == ItemType.BaconRaw || storedItem.type == ItemType.ChickenRaw)
            return player.heldItem.IsEmpty; // tap to cook

        if (storedItem.type == ItemType.PattyCooked || storedItem.type == ItemType.BaconCooked || storedItem.type == ItemType.ChickenCooked)
            return player.heldItem.IsEmpty; // pick up

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.BaconRaw)
            {
                storedItem.Set(ItemType.BaconRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed raw bacon on stove");
                return;
            }

            if (player.heldItem.type == ItemType.ChickenRaw)
            {
                storedItem.Set(ItemType.ChickenRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed raw chicken on stove");
                return;
            }

            storedItem.Set(ItemType.PattyRaw);
            player.heldItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Placed raw patty on stove");
            return;
        }

        if (storedItem.type == ItemType.PattyRaw)
        {
            storedItem.Set(ItemType.PattyCooked);
            UpdateStoredItemVisual();
            Show(player, "Patty cooked");
            return;
        }

        if (storedItem.type == ItemType.BaconRaw)
        {
            storedItem.Set(ItemType.BaconCooked);
            UpdateStoredItemVisual();
            Show(player, "Bacon cooked");
            return;
        }

        if (storedItem.type == ItemType.ChickenRaw)
        {
            storedItem.Set(ItemType.ChickenCooked);
            UpdateStoredItemVisual();
            Show(player, "Chicken cooked");
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

        if (storedItem.type == ItemType.BaconCooked)
        {
            player.heldItem.Set(ItemType.BaconCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Picked up cooked bacon");
            return;
        }

        if (storedItem.type == ItemType.ChickenCooked)
        {
            player.heldItem.Set(ItemType.ChickenCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Picked up cooked chicken");
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
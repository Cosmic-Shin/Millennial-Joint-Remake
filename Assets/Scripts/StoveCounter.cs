using UnityEngine;

public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    // Valid interactions:
    //   - Stove empty + holding PattyRaw or BaconRaw  → place it
    //   - Stove has PattyRaw or BaconRaw + empty hands → cook it
    //   - Stove has PattyCooked or BaconCooked + empty hands → pick it up
    //   - Cannot interact if holding a complete drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        if (player.heldItem.IsCompleteDrink) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.PattyRaw || player.heldItem.type == ItemType.BaconRaw;

        if (storedItem.type == ItemType.PattyRaw || storedItem.type == ItemType.BaconRaw)
            return player.heldItem.IsEmpty; // tap to cook

        if (storedItem.type == ItemType.PattyCooked || storedItem.type == ItemType.BaconCooked)
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
                Show(player, "Placed raw bacon on stove");
                return;
            }

            storedItem.Set(ItemType.PattyRaw);
            player.heldItem.Clear();
            Show(player, "Placed raw patty on stove");
            return;
        }

        if (storedItem.type == ItemType.PattyRaw)
        {
            storedItem.Set(ItemType.PattyCooked);
            Show(player, "Patty cooked");
            return;
        }

        if (storedItem.type == ItemType.BaconRaw)
        {
            storedItem.Set(ItemType.BaconCooked);
            Show(player, "Bacon cooked");
            return;
        }

        if (storedItem.type == ItemType.PattyCooked)
        {
            player.heldItem.Set(ItemType.PattyCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked patty");
            return;
        }

        if (storedItem.type == ItemType.BaconCooked)
        {
            player.heldItem.Set(ItemType.BaconCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked bacon");
            return;
        }

        Show(player, "Cannot use stove now");
    }
}
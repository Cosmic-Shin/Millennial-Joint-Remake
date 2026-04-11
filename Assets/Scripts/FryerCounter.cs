using UnityEngine;

public class FryerCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    // Valid interactions:
    //   - Fryer empty + holding FrozenFries or ChickenRaw  → place item in fryer
    //   - Fryer has FrozenFries or ChickenRaw + empty hands  → cook it
    //   - Fryer has FriesCooked or ChickenCooked + empty hands  → pick up cooked item
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.FrozenFries || player.heldItem.type == ItemType.ChickenRaw;

        if (storedItem.type == ItemType.FrozenFries || storedItem.type == ItemType.ChickenRaw)
            return player.heldItem.IsEmpty;

        if (storedItem.type == ItemType.FriesCooked || storedItem.type == ItemType.ChickenCooked)
            return player.heldItem.IsEmpty;

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.ChickenRaw)
            {
                storedItem.Set(ItemType.ChickenRaw);
                player.heldItem.Clear();
                Show(player, "Placed raw chicken in fryer");
                return;
            }

            storedItem.Set(ItemType.FrozenFries);
            player.heldItem.Clear();
            Show(player, "Placed frozen fries in fryer");
            return;
        }

        if (storedItem.type == ItemType.FrozenFries)
        {
            storedItem.Set(ItemType.FriesCooked);
            Show(player, "Fries cooked");
            return;
        }

        if (storedItem.type == ItemType.ChickenRaw)
        {
            storedItem.Set(ItemType.ChickenCooked);
            Show(player, "Chicken cooked");
            return;
        }

        if (storedItem.type == ItemType.FriesCooked)
        {
            player.heldItem.Set(ItemType.FriesCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked fries");
            return;
        }

        if (storedItem.type == ItemType.ChickenCooked)
        {
            player.heldItem.Set(ItemType.ChickenCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked chicken");
            return;
        }

        Show(player, "Cannot use fryer now");
    }
}

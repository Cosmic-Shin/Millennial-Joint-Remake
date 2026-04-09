using UnityEngine;

public class FryerCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    // Valid interactions:
    //   - Fryer empty + holding FrozenFries  → place fries in fryer
    //   - Fryer has FrozenFries + empty hands  → cook fries
    //   - Fryer has FriesCooked + empty hands  → pick up cooked fries
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.FrozenFries;

        if (storedItem.type == ItemType.FrozenFries)
            return player.heldItem.IsEmpty;

        if (storedItem.type == ItemType.FriesCooked)
            return player.heldItem.IsEmpty;

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
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

        if (storedItem.type == ItemType.FriesCooked)
        {
            player.heldItem.Set(ItemType.FriesCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked fries");
            return;
        }

        Show(player, "Cannot use fryer now");
    }
}

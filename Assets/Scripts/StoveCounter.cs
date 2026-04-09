using UnityEngine;

public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    // Valid interactions:
    //   - Stove empty + holding PattyRaw  → place it
    //   - Stove has PattyRaw + empty hands → cook it
    //   - Stove has PattyCooked + empty hands → pick it up
    //   - Cannot interact if holding a complete drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        if (player.heldItem.IsCompleteDrink) return false;

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

        if (storedItem.IsEmpty)
        {
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

        if (storedItem.type == ItemType.PattyCooked)
        {
            player.heldItem.Set(ItemType.PattyCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked patty");
            return;
        }

        Show(player, "Cannot use stove now");
    }
}
using UnityEngine;

public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.PattyRaw)
            {
                storedItem.Set(ItemType.PattyRaw);
                player.heldItem.Clear();
                Show(player, "Placed raw patty on stove");
            }
            else
            {
                Show(player, "Need raw patty first");
            }
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
            if (!player.heldItem.IsEmpty)
            {
                Show(player, "Hands are full");
                return;
            }

            player.heldItem.Set(ItemType.PattyCooked);
            storedItem.Clear();
            Show(player, "Picked up cooked patty");
            return;
        }

        Show(player, "Cannot use stove now");
    }
}
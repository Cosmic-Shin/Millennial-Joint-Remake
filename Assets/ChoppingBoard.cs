using UnityEngine;

public class ChoppingBoard : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.VeggieRaw)
            {
                storedItem.Set(ItemType.VeggieRaw);
                player.heldItem.Clear();
                Show(player, "Placed raw veggie on chopping board");
            }
            else
            {
                Show(player, "Need raw veggie first");
            }
            return;
        }

        if (storedItem.type == ItemType.VeggieRaw)
        {
            storedItem.Set(ItemType.VeggieChopped);
            Show(player, "Veggie chopped");
            return;
        }

        if (storedItem.type == ItemType.VeggieChopped)
        {
            if (!player.heldItem.IsEmpty)
            {
                Show(player, "Hands are full");
                return;
            }

            player.heldItem.Set(ItemType.VeggieChopped);
            storedItem.Clear();
            Show(player, "Picked up chopped veggie");
            return;
        }

        Show(player, "Cannot use chopping board now");
    }
}
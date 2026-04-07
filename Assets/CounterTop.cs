using UnityEngine;

public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        // If countertop is empty, only a plate can be placed
        if (storedItem.IsEmpty)
        {
            if (player.heldItem.IsEmpty)
            {
                Show(player, "Need a plate first");
                return;
            }

            if (player.heldItem.IsPlate)
            {
                storedItem.CopyFrom(player.heldItem);
                player.heldItem.Clear();
                Show(player, "Placed plate on countertop");
                return;
            }

            if (player.heldItem.type == ItemType.Bun)
            {
                Show(player, "Place a plate first");
                return;
            }

            if (player.heldItem.type == ItemType.PattyRaw)
            {
                Show(player, "Raw patty must be cooked first");
                return;
            }

            if (player.heldItem.type == ItemType.PattyCooked)
            {
                Show(player, "Place a plate first");
                return;
            }

            if (player.heldItem.type == ItemType.VeggieRaw)
            {
                Show(player, "Raw veggie must be chopped first");
                return;
            }

            if (player.heldItem.type == ItemType.VeggieChopped)
            {
                Show(player, "Place a plate first");
                return;
            }

            Show(player, "Need a plate first");
            return;
        }

        // Countertop should only store a plate
        if (!storedItem.IsPlate)
        {
            Show(player, "Countertop error: only plate should be on countertop");
            return;
        }

        // Empty hands = pick up the plate
        if (player.heldItem.IsEmpty)
        {
            player.heldItem.CopyFrom(storedItem);
            storedItem.Clear();
            Show(player, "Picked up plate from countertop");
            return;
        }

        // Can't place another plate
        if (player.heldItem.IsPlate)
        {
            Show(player, "Counter already has a plate");
            return;
        }

        // ORDER RULE: Bun first
        if (player.heldItem.type == ItemType.Bun)
        {
            if (storedItem.plateHasBun)
            {
                Show(player, "Plate already has bun");
                return;
            }

            storedItem.plateHasBun = true;
            player.heldItem.Clear();
            Show(player, "Placed bun on plate");
            return;
        }

        // ORDER RULE: Patty second, only after bun
        if (player.heldItem.type == ItemType.PattyCooked)
        {
            if (!storedItem.plateHasBun)
            {
                Show(player, "Place bun first");
                return;
            }

            if (storedItem.plateHasPatty)
            {
                Show(player, "Plate already has cooked patty");
                return;
            }

            storedItem.plateHasPatty = true;
            player.heldItem.Clear();
            Show(player, "Placed cooked patty on plate");
            return;
        }

        // ORDER RULE: Veggie third, only after bun + patty
        if (player.heldItem.type == ItemType.VeggieChopped)
        {
            if (!storedItem.plateHasBun)
            {
                Show(player, "Place bun first");
                return;
            }

            if (!storedItem.plateHasPatty)
            {
                Show(player, "Place cooked patty second");
                return;
            }

            if (storedItem.plateHasVeggie)
            {
                Show(player, "Plate already has chopped veggie");
                return;
            }

            storedItem.plateHasVeggie = true;
            player.heldItem.Clear();
            Show(player, "Placed chopped veggie on plate");
            return;
        }

        if (player.heldItem.type == ItemType.PattyRaw)
        {
            Show(player, "Raw patty must be cooked first");
            return;
        }

        if (player.heldItem.type == ItemType.VeggieRaw)
        {
            Show(player, "Raw veggie must be chopped first");
            return;
        }

        Show(player, "Wrong item for plate");
    }
}
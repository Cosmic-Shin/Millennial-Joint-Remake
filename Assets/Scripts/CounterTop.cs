using UnityEngine;

public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    // ------------------------------------------------------------------
    // Valid interactions:
    //   - Counter empty + holding Plate → place plate
    //   - Counter has Plate + holding valid ingredient → add ingredient
    //   - Counter has Plate + empty hands → pick up plate
    //      all that and bla bla bla
    // ------------------------------------------------------------------
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        // Place a plate onto empty counter
        if (storedItem.IsEmpty)
            return player.heldItem.IsPlate;

        // Counter must have a plate to do anything further
        if (!storedItem.IsPlate) return false;

        // Pick up plate
        if (player.heldItem.IsEmpty) return true;

        // Add Bun (must not already have one)
        if (player.heldItem.type == ItemType.Bun && !storedItem.plateHasBun)
            return true;

        // Add PattyCooked (requires Bun first, must not already have one)
        if (player.heldItem.type == ItemType.PattyCooked
            && storedItem.plateHasBun && !storedItem.plateHasPatty)
            return true;

        // Add VeggieChopped (requires Bun + Patty, must not already have one)
        if (player.heldItem.type == ItemType.VeggieChopped
            && storedItem.plateHasBun && storedItem.plateHasPatty && !storedItem.plateHasVeggie)
            return true;

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            TryPlacePlate(player);
            return;
        }

        if (!storedItem.IsPlate)
        {
            // Should never happen if CanInteractWith is working, but guard anyway
            Show(player, "Countertop error: unexpected item on counter");
            return;
        }

        // Empty hands → pick up plate
        if (player.heldItem.IsEmpty)
        {
            player.heldItem.CopyFrom(storedItem);
            storedItem.Clear();
            Show(player, "Picked up plate from countertop");
            return;
        }

        TryAddIngredient(player);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void TryPlacePlate(PlayerControl player)
    {
        if (!player.heldItem.IsPlate)
        {
            Show(player, "Place a plate first");
            return;
        }

        storedItem.CopyFrom(player.heldItem);
        player.heldItem.Clear();
        Show(player, "Placed plate on countertop");
    }

    private void TryAddIngredient(PlayerControl player)
    {
        switch (player.heldItem.type)
        {
            case ItemType.Bun:
                if (storedItem.plateHasBun)
                {
                    Show(player, "Plate already has bun");
                    return;
                }
                storedItem.plateHasBun = true;
                player.heldItem.Clear();
                Show(player, "Placed bun on plate");
                break;

            case ItemType.PattyCooked:
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
                break;

            case ItemType.VeggieChopped:
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
                break;

            case ItemType.PattyRaw:
                Show(player, "Raw patty must be cooked first");
                break;

            case ItemType.VeggieRaw:
                Show(player, "Raw veggie must be chopped first");
                break;

            default:
                Show(player, "Wrong item for plate");
                break;
        }
    }
}
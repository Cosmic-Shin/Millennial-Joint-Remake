using UnityEngine;

public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();

    // ------------------------------------------------------------------
    // Valid interactions:
    //   - Counter empty + holding Plate → place plate
    //   - Counter empty + holding complete Cup → place cup
    //   - Counter has Plate + holding valid ingredient → add ingredient
    //   - Counter has Plate + empty hands → pick up plate
    //   - Counter has Cup + empty hands → pick up cup
    //      all that and bla bla bla
    // ------------------------------------------------------------------
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        // Place a plate or cup onto empty counter
        if (storedItem.IsEmpty)
            return player.heldItem.IsPlate || player.heldItem.IsCompleteDrink;

        // Counter must have a plate to do anything further (burger/fries building)
        if (storedItem.IsPlate)
        {
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

            // Add Bacon (requires Bun + Patty + Veggie, must not already have bacon)
            if (player.heldItem.type == ItemType.BaconCooked
                && storedItem.plateHasBun && storedItem.plateHasPatty && storedItem.plateHasVeggie && !storedItem.plateHasBacon)
                return true;

            // Add cooked fries to an empty plate
            if (player.heldItem.type == ItemType.FriesCooked
                && !storedItem.plateHasFries
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasBacon
                && !storedItem.plateHasChicken)
                return true;

            // Add cooked chicken to an empty plate
            if (player.heldItem.type == ItemType.ChickenCooked
                && !storedItem.plateHasFries
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasBacon
                && !storedItem.plateHasChicken)
                return true;

            return false;
        }

        // Counter has a cup - can only pick up if hands are empty
        if (storedItem.IsCup)
            return player.heldItem.IsEmpty;

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.IsPlate)
            {
                TryPlacePlate(player);
            }
            else if (player.heldItem.IsCompleteDrink)
            {
                TryPlaceCup(player);
            }
            return;
        }

        if (storedItem.IsPlate)
        {
            // Empty hands → pick up plate
            if (player.heldItem.IsEmpty)
            {
                player.heldItem.CopyFrom(storedItem);
                storedItem.Clear();
                Show(player, "Picked up plate from countertop");
                return;
            }

            TryAddIngredient(player);
            return;
        }

        if (storedItem.IsCup)
        {
            // Empty hands → pick up cup
            if (player.heldItem.IsEmpty)
            {
                player.heldItem.CopyFrom(storedItem);
                storedItem.Clear();
                Show(player, "Picked up cup from countertop");
                return;
            }
        }

        Show(player, "Countertop error: invalid interaction");
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

    private void TryPlaceCup(PlayerControl player)
    {
        if (!player.heldItem.IsCompleteDrink)
        {
            Show(player, "Complete the drink first");
            return;
        }

        storedItem.CopyFrom(player.heldItem);
        player.heldItem.Clear();
        Show(player, "Placed cup on countertop");
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

            case ItemType.FriesCooked:
                if (storedItem.plateHasFries)
                {
                    Show(player, "Plate already has fries");
                    return;
                }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasBacon || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add fries to a burger or chicken plate");
                    return;
                }
                storedItem.plateHasFries = true;
                player.heldItem.Clear();
                Show(player, "Placed fries on plate");
                break;

            case ItemType.BaconCooked:
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
                if (!storedItem.plateHasVeggie)
                {
                    Show(player, "Place chopped veggie third");
                    return;
                }
                if (storedItem.plateHasBacon)
                {
                    Show(player, "Plate already has bacon");
                    return;
                }
                if (storedItem.plateHasFries || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add bacon to this plate");
                    return;
                }
                storedItem.plateHasBacon = true;
                player.heldItem.Clear();
                Show(player, "Placed bacon on plate");
                break;

            case ItemType.ChickenCooked:
                if (storedItem.plateHasFries || storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasBacon || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add cooked chicken to this plate");
                    return;
                }
                storedItem.plateHasChicken = true;
                player.heldItem.Clear();
                Show(player, "Placed cooked chicken on plate");
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
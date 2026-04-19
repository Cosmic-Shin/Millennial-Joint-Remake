using UnityEngine;

public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // ------------------------------------------------------------------
    // Valid interactions:
    //   - Counter empty + holding Plate → place plate
    //   - Counter has Plate + holding valid ingredient → add ingredient
    //   - Counter has Plate + empty hands → pick up plate
    // ------------------------------------------------------------------
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        // Place a plate onto empty counter
        if (storedItem.IsEmpty)
            return player.heldItem.IsPlate;

        // Counter must have a plate to do anything further (burger/fries building)
        if (storedItem.IsPlate)
        {
            if (player.heldItem.IsEmpty) return true;

            if (player.heldItem.type == ItemType.Bun && !storedItem.plateHasBun)
                return true;

            if (player.heldItem.type == ItemType.Bread
                && !storedItem.plateHasBread
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
                && !storedItem.plateHasCheese
                && !storedItem.plateHasFries
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.PattyCooked
                && storedItem.plateHasBun && !storedItem.plateHasPatty)
                return true;

            if (player.heldItem.type == ItemType.VeggieChopped
                && storedItem.plateHasBun && storedItem.plateHasPatty && !storedItem.plateHasVeggie)
                return true;

            if (player.heldItem.type == ItemType.HamCooked
                && storedItem.plateHasBread && !storedItem.plateHasHam
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasFries
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.Cheese
                && storedItem.plateHasBread && storedItem.plateHasHam
                && !storedItem.plateHasCheese
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasFries
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.FriesCooked
                && !storedItem.plateHasFries
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
                && !storedItem.plateHasCheese
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.ChickenCooked
                && !storedItem.plateHasFries
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
                && !storedItem.plateHasCheese
                && !storedItem.plateHasChicken)
                return true;

            return false;
        }

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
            return;
        }

        if (storedItem.IsPlate)
        {
            // Empty hands → pick up plate
            if (player.heldItem.IsEmpty)
            {
                player.heldItem.CopyFrom(storedItem);
                storedItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Picked up plate from countertop");
                return;
            }

            TryAddIngredient(player);
            return;
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
        UpdateStoredItemVisual();
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
                UpdateStoredItemVisual();
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
                UpdateStoredItemVisual();
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
                UpdateStoredItemVisual();
                Show(player, "Placed chopped veggie on plate");
                break;

            case ItemType.Bread:
                if (storedItem.plateHasBread)
                {
                    Show(player, "Plate already has bread");
                    return;
                }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasHam || storedItem.plateHasCheese || storedItem.plateHasFries || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add bread to this plate");
                    return;
                }
                storedItem.plateHasBread = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed bread on plate");
                break;

            case ItemType.HamCooked:
                if (!storedItem.plateHasBread)
                {
                    Show(player, "Place bread first");
                    return;
                }
                if (storedItem.plateHasHam)
                {
                    Show(player, "Plate already has cooked ham");
                    return;
                }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasFries || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add ham to this plate");
                    return;
                }
                storedItem.plateHasHam = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed cooked ham on plate");
                break;

            case ItemType.Cheese:
                if (!storedItem.plateHasBread)
                {
                    Show(player, "Place bread first");
                    return;
                }
                if (!storedItem.plateHasHam)
                {
                    Show(player, "Place cooked ham before cheese");
                    return;
                }
                if (storedItem.plateHasCheese)
                {
                    Show(player, "Plate already has cheese");
                    return;
                }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasFries || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add cheese to this plate");
                    return;
                }
                storedItem.plateHasCheese = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed cheese on plate");
                break;

            case ItemType.FriesCooked:
                if (storedItem.plateHasFries)
                {
                    Show(player, "Plate already has fries");
                    return;
                }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasHam || storedItem.plateHasCheese || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add fries to a burger or chicken plate");
                    return;
                }
                storedItem.plateHasFries = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed fries on plate");
                break;

            case ItemType.ChickenCooked:
                if (storedItem.plateHasFries || storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie || storedItem.plateHasHam || storedItem.plateHasCheese || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add cooked chicken to this plate");
                    return;
                }
                storedItem.plateHasChicken = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed cooked chicken on plate");
                break;

            case ItemType.PattyRaw:
                Show(player, "Raw patty must be cooked first");
                break;

            case ItemType.VeggieRaw:
                Show(player, "Raw veggie must be chopped first");
                break;

            case ItemType.HamRaw:
                Show(player, "Raw ham must be cooked first");
                break;

            default:
                Show(player, "Wrong item for plate");
                break;
        }
    }

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}
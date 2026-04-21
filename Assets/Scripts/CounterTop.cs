using UnityEngine;

public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // ------------------------------------------------------------------
    // BURGER:   Bun → PattyCooked → VeggieRaw  (complete)
    // SANDWICH: Bread → HamRaw (Ham & Cheese combined = complete)
    // SIDES:    FriesCooked alone | ChickenCooked alone
    // ------------------------------------------------------------------
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.IsPlate;

        if (storedItem.IsPlate)
        {
            if (player.heldItem.IsEmpty) return true;

            // --- Burger path ---
            if (player.heldItem.type == ItemType.Bun && !storedItem.plateHasBun)
                return true;

            if (player.heldItem.type == ItemType.PattyCooked
                && storedItem.plateHasBun && !storedItem.plateHasPatty)
                return true;

            if (player.heldItem.type == ItemType.VeggieRaw
                && storedItem.plateHasBun && storedItem.plateHasPatty && !storedItem.plateHasVeggie)
                return true;

            // --- Sandwich path: Bread → Ham & Cheese (done) ---
            if (player.heldItem.type == ItemType.Bread
                && !storedItem.plateHasBread
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
                && !storedItem.plateHasFries
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.HamRaw
                && storedItem.plateHasBread && !storedItem.plateHasHam
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasFries
                && !storedItem.plateHasChicken)
                return true;

            // --- Sides ---
            if (player.heldItem.type == ItemType.FriesCooked
                && !storedItem.plateHasFries
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.ChickenCooked
                && !storedItem.plateHasFries
                && !storedItem.plateHasBun
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
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
                TryPlacePlate(player);
            return;
        }

        if (storedItem.IsPlate)
        {
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

    private void TryPlacePlate(PlayerControl player)
    {
        storedItem.CopyFrom(player.heldItem);
        player.heldItem.Clear();
        UpdateStoredItemVisual();
        Show(player, "Placed plate on countertop");
    }

    private void TryAddIngredient(PlayerControl player)
    {
        switch (player.heldItem.type)
        {
            // ---- Burger ----
            case ItemType.Bun:
                if (storedItem.plateHasBun) { Show(player, "Plate already has bun"); return; }
                storedItem.plateHasBun = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed bun on plate");
                break;

            case ItemType.PattyCooked:
                if (!storedItem.plateHasBun) { Show(player, "Place bun first"); return; }
                if (storedItem.plateHasPatty) { Show(player, "Plate already has patty"); return; }
                storedItem.plateHasPatty = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed cooked patty on plate");
                break;

            case ItemType.VeggieRaw:
                if (!storedItem.plateHasBun) { Show(player, "Place bun first"); return; }
                if (!storedItem.plateHasPatty) { Show(player, "Place patty second"); return; }
                if (storedItem.plateHasVeggie) { Show(player, "Plate already has veggie"); return; }
                storedItem.plateHasVeggie = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed veggie on plate");
                break;

            // ---- Sandwich ----
            case ItemType.Bread:
                if (storedItem.plateHasBread) { Show(player, "Plate already has bread"); return; }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie
                    || storedItem.plateHasHam || storedItem.plateHasFries || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add bread to this plate");
                    return;
                }
                storedItem.plateHasBread = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed bread on plate");
                break;

            case ItemType.HamRaw:  // Ham & Cheese combined — completes sandwich
                if (!storedItem.plateHasBread) { Show(player, "Place bread first"); return; }
                if (storedItem.plateHasHam) { Show(player, "Plate already has Ham & Cheese"); return; }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie
                    || storedItem.plateHasFries || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add Ham & Cheese to this plate");
                    return;
                }
                storedItem.plateHasHam = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed Ham & Cheese on plate — Sandwich complete!");
                break;

            // ---- Sides ----
            case ItemType.FriesCooked:
                if (storedItem.plateHasFries) { Show(player, "Plate already has fries"); return; }
                if (storedItem.plateHasBun || storedItem.plateHasPatty || storedItem.plateHasVeggie
                    || storedItem.plateHasHam || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add fries to this plate");
                    return;
                }
                storedItem.plateHasFries = true;
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                Show(player, "Placed fries on plate");
                break;

            case ItemType.ChickenCooked:
                if (storedItem.plateHasFries || storedItem.plateHasBun || storedItem.plateHasPatty
                    || storedItem.plateHasVeggie || storedItem.plateHasHam || storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add chicken to this plate");
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
using UnityEngine;

public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // ------------------------------------------------------------------
    // BURGER:   Bun → PattyCooked → VeggieRaw  (complete)
    // SANDWICH: Bread → HamRaw (Ham & Cheese combined = complete)
    // SIDES:    FriesCooked alone | ChickenCooked alone
    // CHILI DOG: DogBun → HotDogCooked → (ChiliPot adds chili)
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

            // --- Sandwich path ---
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

            // --- CHILI DOG ---
            if (player.heldItem.type == ItemType.DogBun
                && !storedItem.plateHasDogBun
                && !storedItem.plateHasBun
                && !storedItem.plateHasBread
                && !storedItem.plateHasPatty
                && !storedItem.plateHasVeggie
                && !storedItem.plateHasHam
                && !storedItem.plateHasFries
                && !storedItem.plateHasChicken)
                return true;

            if (player.heldItem.type == ItemType.HotDogCooked
                && storedItem.plateHasDogBun
                && !storedItem.plateHasHotdog)
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
            // ---- BURGER ----
            case ItemType.Bun:
                if (storedItem.plateHasBun) { Show(player, "Plate already has bun"); return; }
                storedItem.plateHasBun = true;
                break;

            case ItemType.PattyCooked:
                if (!storedItem.plateHasBun) { Show(player, "Place bun first"); return; }
                if (storedItem.plateHasPatty) { Show(player, "Plate already has patty"); return; }
                storedItem.plateHasPatty = true;
                break;

            case ItemType.VeggieRaw:
                if (!storedItem.plateHasPatty) { Show(player, "Place patty first"); return; }
                if (storedItem.plateHasVeggie) { Show(player, "Plate already has veggie"); return; }
                storedItem.plateHasVeggie = true;
                break;

            // ---- SANDWICH ----
            case ItemType.Bread:
                if (storedItem.plateHasBread) { Show(player, "Plate already has bread"); return; }
                storedItem.plateHasBread = true;
                break;

            case ItemType.HamRaw:
                if (!storedItem.plateHasBread) { Show(player, "Place bread first"); return; }
                if (storedItem.plateHasHam) { Show(player, "Plate already has Ham & Cheese"); return; }
                storedItem.plateHasHam = true;
                break;

            // ---- SIDES ----
            case ItemType.FriesCooked:
                if (storedItem.plateHasFries) { Show(player, "Plate already has fries"); return; }
                storedItem.plateHasFries = true;
                break;

            case ItemType.ChickenCooked:
                if (storedItem.plateHasChicken) { Show(player, "Plate already has chicken"); return; }
                storedItem.plateHasChicken = true;
                break;

            // ---- CHILI DOG ----
            case ItemType.DogBun:
                if (storedItem.plateHasDogBun) { Show(player, "Plate already has dog bun"); return; }

                if (storedItem.plateHasBun || storedItem.plateHasBread ||
                    storedItem.plateHasPatty || storedItem.plateHasVeggie ||
                    storedItem.plateHasHam || storedItem.plateHasFries ||
                    storedItem.plateHasChicken)
                {
                    Show(player, "Cannot add dog bun to this plate");
                    return;
                }

                storedItem.plateHasDogBun = true;
                break;

            case ItemType.HotDogCooked:
                if (!storedItem.plateHasDogBun) { Show(player, "Place dog bun first"); return; }
                if (storedItem.plateHasHotdog) { Show(player, "Plate already has hotdog"); return; }
                storedItem.plateHasHotdog = true;
                break;

            case ItemType.PattyRaw:
                Show(player, "Raw patty must be cooked first");
                return;

            default:
                Show(player, "Wrong item for plate");
                return;
        }

        player.heldItem.Clear();
        UpdateStoredItemVisual();
    }

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}
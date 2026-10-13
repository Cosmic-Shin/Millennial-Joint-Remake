public class CounterTop : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    [System.Flags]
    private enum Contents
    {
        None = 0, Bun = 1, Patty = 2, Veggie = 4, Bread = 8, Ham = 16,
        Fries = 32, Chicken = 64, DogBun = 128, Hotdog = 256, Chili = 512
    }

    private static Contents GetContents(KitchenItemData plate)
    {
        Contents c = Contents.None;
        if (plate.plateHasBun) c |= Contents.Bun;
        if (plate.plateHasPatty) c |= Contents.Patty;
        if (plate.plateHasVeggie) c |= Contents.Veggie;
        if (plate.plateHasBread) c |= Contents.Bread;
        if (plate.plateHasHam) c |= Contents.Ham;
        if (plate.plateHasFries) c |= Contents.Fries;
        if (plate.plateHasChicken) c |= Contents.Chicken;
        if (plate.plateHasDogBun) c |= Contents.DogBun;
        if (plate.plateHasHotdog) c |= Contents.Hotdog;
        if (plate.plateHasChili) c |= Contents.Chili;
        return c;
    }

    private static bool CanAdd(KitchenItemData plate, ItemType ingredient)
    {
        Contents c = GetContents(plate);

        return ingredient switch
        {
            ItemType.Bun => c == Contents.None,
            ItemType.PattyCooked => c == Contents.Bun,
            ItemType.VeggieRaw => c == (Contents.Bun | Contents.Patty),
            ItemType.Bread => c == Contents.None,
            ItemType.HamRaw => c == Contents.Bread,
            ItemType.FriesCooked => c == Contents.None,
            ItemType.ChickenCooked => c == Contents.None,
            ItemType.DogBun => c == Contents.None,
            ItemType.HotDogCooked => c == Contents.DogBun,
            _ => false
        };
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.IsPlate;

        if (storedItem.IsPlate)
            return player.heldItem.IsEmpty || CanAdd(storedItem, player.heldItem.type);

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
        ItemType ingredient = player.heldItem.type;

        if (!CanAdd(storedItem, ingredient))
        {
            Show(player, ingredient == ItemType.PattyRaw || ingredient == ItemType.HotDogRaw
                ? "Cook it first"
                : "Wrong item for this plate");
            return;
        }

        switch (ingredient)
        {
            case ItemType.Bun: storedItem.plateHasBun = true; break;
            case ItemType.PattyCooked: storedItem.plateHasPatty = true; break;
            case ItemType.VeggieRaw: storedItem.plateHasVeggie = true; break;
            case ItemType.Bread: storedItem.plateHasBread = true; break;
            case ItemType.HamRaw: storedItem.plateHasHam = true; break;
            case ItemType.FriesCooked: storedItem.plateHasFries = true; break;
            case ItemType.ChickenCooked: storedItem.plateHasChicken = true; break;
            case ItemType.DogBun: storedItem.plateHasDogBun = true; break;
            case ItemType.HotDogCooked: storedItem.plateHasHotdog = true; break;
        }

        player.heldItem.Clear();
        UpdateStoredItemVisual();
        AudioManager.Instance?.PlayIngredientOnPlateSFX();
    }

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}

using UnityEngine;

public class ChoppingBoard : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    // Valid interactions:
    //   - Board empty + holding VeggieRaw → place it
    //   - Board has VeggieRaw + empty hands → chop it
    //   - Board has VeggieChopped + empty hands → pick it up
    //   - Cannot interact if holding a complete drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.VeggieRaw;

        if (storedItem.type == ItemType.VeggieRaw)
            return player.heldItem.IsEmpty; // tap to chop

        if (storedItem.type == ItemType.VeggieChopped)
            return player.heldItem.IsEmpty; // pick up

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        Debug.Log("ChoppingBoard.Interact called | storedItem: "
                  + storedItem.GetDisplayName()
                  + " | player before: " + player.GetHeldItemDebug());

        if (storedItem.IsEmpty)
        {
            storedItem.Set(ItemType.VeggieRaw);
            player.heldItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Placed raw veggie on chopping board");
            Debug.Log("ChoppingBoard result | storedItem now: " + storedItem.GetDisplayName());
            return;
        }

        if (storedItem.type == ItemType.VeggieRaw)
        {
            storedItem.Set(ItemType.VeggieChopped);
            UpdateStoredItemVisual();
            Show(player, "Veggie chopped");
            Debug.Log("ChoppingBoard result | storedItem now: " + storedItem.GetDisplayName());
            return;
        }

        if (storedItem.type == ItemType.VeggieChopped)
        {
            player.heldItem.Set(ItemType.VeggieChopped);
            storedItem.Clear();
            UpdateStoredItemVisual();
            Show(player, "Picked up chopped veggie");
            Debug.Log("ChoppingBoard result | player after: " + player.GetHeldItemDebug());
            return;
        }

        Show(player, "Cannot use chopping board now");
    }

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}
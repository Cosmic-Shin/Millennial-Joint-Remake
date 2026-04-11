using UnityEngine;

public class ServingCounter : BaseStation, IInteractable
{
    public int totalServed;

    // Only valid when player holds a complete burger, bacon burger, fried chicken, fries plate, or complete drink
    public bool CanInteractWith(PlayerControl player)
    {
        return player != null
            && ((player.heldItem.IsPlate && (player.heldItem.IsCompleteBurger || player.heldItem.IsCompleteBaconBurger || player.heldItem.IsCompleteFriedChicken || player.heldItem.IsCompleteFries))
                || player.heldItem.IsCompleteDrink);
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (OrderManager.Instance != null)
        {
            if (!OrderManager.Instance.TryServeItem(player.heldItem))
            {
                Show(player, "Item is not part of the current order");
                return;
            }
        }

        string servedName = player.heldItem.GetDisplayName();
        player.heldItem.Clear();
        totalServed++;
        Show(player, servedName + " served! Total served: " + totalServed);
    }
}
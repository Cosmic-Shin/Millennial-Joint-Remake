using UnityEngine;

public class ServingCounter : BaseStation, IInteractable
{
    public int totalServed;

    // Accepts complete plates OR any filled cup (Ice Tea, Soda, Orange Juice)
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        bool hasCompletePlate = player.heldItem.IsPlate &&
            (player.heldItem.IsCompleteBurger ||
             player.heldItem.IsCompleteSandwich ||
             player.heldItem.IsCompleteFriedChicken ||
             player.heldItem.IsCompleteFries);

        bool hasCompleteDrink = player.heldItem.IsCompleteDrink;

        return hasCompletePlate || hasCompleteDrink;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        bool served = false;
        if (OrderManager.Instance != null)
            served = OrderManager.Instance.TryServeItem(player.heldItem);
        else
            served = true;

        if (!served)
        {
            Show(player, player.heldItem.GetDisplayName() + " is not part of the current order");
            return;
        }

        string servedName = player.heldItem.GetDisplayName();
        player.heldItem.Clear();
        player.RefreshHeldItemDisplay();
        totalServed++;
        Show(player, servedName + " served! Total served: " + totalServed);
    }
}
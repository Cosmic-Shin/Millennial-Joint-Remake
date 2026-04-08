using UnityEngine;

public class ServingCounter : BaseStation, IInteractable
{
    public int totalServed;

    // Only valid when player holds a complete sandwich
    public bool CanInteractWith(PlayerControl player)
    {
        return player != null
            && player.heldItem.IsPlate
            && player.heldItem.IsCompleteSandwich;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        totalServed++;
        player.heldItem.Clear();
        Show(player, "Sandwich served! Total served: " + totalServed);
    }
}
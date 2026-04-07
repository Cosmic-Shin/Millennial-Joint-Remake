using UnityEngine;

public class ServingCounter : BaseStation, IInteractable
{
    public int totalServed;

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!player.heldItem.IsPlate)
        {
            Show(player, "Need a plated sandwich");
            return;
        }

        if (!player.heldItem.IsCompleteSandwich)
        {
            Show(player, "Plate is not complete");
            return;
        }

        totalServed++;
        player.heldItem.Clear();
        Show(player, "Sandwich served! Total served: " + totalServed);
    }
}
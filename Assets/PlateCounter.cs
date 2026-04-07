using UnityEngine;

public class PlateCounter : BaseStation, IInteractable
{
    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!player.heldItem.IsEmpty)
        {
            Show(player, "Hands must be empty to take a plate");
            return;
        }

        player.heldItem.MakePlate();
        Show(player, "Picked up plate");
    }
}
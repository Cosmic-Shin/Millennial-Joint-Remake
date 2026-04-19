using UnityEngine;

public class PlateCounter : BaseStation, IInteractable
{
    // Can only grab a plate when hands are empty
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        Debug.Log("PlateCounter.Interact called | player holding before: " + player.GetHeldItemDebug());
        player.heldItem.MakePlate();
        Debug.Log("PlateCounter.Interact completed | player holding after: " + player.GetHeldItemDebug());
        Show(player, "Picked up plate");
    }
}
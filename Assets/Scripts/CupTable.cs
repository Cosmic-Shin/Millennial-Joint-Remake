using UnityEngine;

public class CupTable : BaseStation, IInteractable
{
    // Can only grab a cup when hands are empty
    public bool CanInteractWith(PlayerControl player)
    {
        return player != null && player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        Debug.Log("CupTable.Interact called | player holding before: " + player.GetHeldItemDebug());
        player.heldItem.Set(ItemType.Cup);
        Debug.Log("CupTable.Interact completed | player holding after: " + player.GetHeldItemDebug());
        Show(player, "Picked up cup");
    }
}

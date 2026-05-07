using UnityEngine;

public class TrashBin : BaseStation, IInteractable
{
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return !player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null || player.heldItem.IsEmpty) return;

        player.heldItem.Clear();
        player.RefreshHeldItemDisplay();
        Show(player, "Trashed item!");

        // Play interact audio (consistent with other stations)
        AudioManager.Instance?.PlayCounterTopInteractSFX();
    }
}

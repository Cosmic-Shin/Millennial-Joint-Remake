using UnityEngine;

public class CupTable : BaseStation, IInteractable
{
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!player.heldItem.IsEmpty)
        {
            Show(player, "Hands must be empty to pick up a cup");
            return;
        }

        player.heldItem.Set(ItemType.Cup);
        player.RefreshHeldItemDisplay();
        Show(player, "Picked up empty cup");
    }
}

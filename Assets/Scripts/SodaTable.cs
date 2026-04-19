using UnityEngine;

public class SodaTable : BaseStation, IInteractable
{
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsCup && !player.heldItem.cupHasSoda && !player.heldItem.cupHasCoffee;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!player.heldItem.IsCup)
        {
            Show(player, "Hold an empty cup to fill soda");
            return;
        }

        if (player.heldItem.cupHasSoda || player.heldItem.cupHasCoffee)
        {
            Show(player, "Cup is already filled");
            return;
        }

        player.heldItem.cupHasSoda = true;
        player.RefreshHeldItemDisplay();
        Show(player, "Filled cup with soda");
    }
}

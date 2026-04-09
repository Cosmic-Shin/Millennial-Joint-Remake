using UnityEngine;

public class SodaTable : BaseStation, IInteractable
{
    // Valid interactions:
    //   - Holding Cup + no boba pearls → add soda and complete the drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (player.heldItem.type != ItemType.Cup) return false;
        if (player.heldItem.cupHasSoda) return false;
        if (player.heldItem.cupHasBoba || player.heldItem.cupBobaDrinkReady) return false;

        return true;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        Debug.Log("SodaTable.Interact called | cup before: " + player.GetHeldItemDebug());

        if (player.heldItem.type != ItemType.Cup)
        {
            Show(player, "Must be holding a cup");
            return;
        }

        if (player.heldItem.cupHasSoda)
        {
            Show(player, "Cup already has soda");
            return;
        }

        if (player.heldItem.cupHasBoba || player.heldItem.cupBobaDrinkReady)
        {
            Show(player, "Cannot add soda to boba drink");
            return;
        }

        player.heldItem.cupHasSoda = true;
        Debug.Log("SodaTable.Interact completed | cup after: " + player.GetHeldItemDebug());
        Show(player, "Soda drink complete!");
    }
}

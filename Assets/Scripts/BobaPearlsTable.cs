using UnityEngine;

public class BobaPearlsTable : BaseStation, IInteractable
{
    // Valid interactions:
    //   - Holding Cup + no soda + no boba drink yet → add boba pearls
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

        Debug.Log("BobaPearlsTable.Interact called | cup before: " + player.GetHeldItemDebug());

        if (player.heldItem.type != ItemType.Cup)
        {
            Show(player, "Must be holding a cup");
            return;
        }

        if (player.heldItem.cupHasSoda)
        {
            Show(player, "Cannot add boba to soda drink");
            return;
        }

        if (player.heldItem.cupHasBoba || player.heldItem.cupBobaDrinkReady)
        {
            Show(player, "Cup already has boba");
            return;
        }

        player.heldItem.cupHasBoba = true;
        Debug.Log("BobaPearlsTable.Interact completed | cup after: " + player.GetHeldItemDebug());
        Show(player, "Added boba pearls to cup");
    }
}

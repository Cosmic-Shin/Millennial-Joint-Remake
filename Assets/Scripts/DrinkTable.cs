using UnityEngine;

public class DrinkTable : BaseStation, IInteractable
{
    // Valid interactions:
    //   - Holding Cup + has boba pearls added → finalize boba drink
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (player.heldItem.type != ItemType.Cup) return false;
        if (!player.heldItem.cupHasBoba) return false;
        if (player.heldItem.cupHasSoda) return false;
        if (player.heldItem.cupBobaDrinkReady) return false;

        return true;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        Debug.Log("DrinkTable.Interact called | cup before: " + player.GetHeldItemDebug());

        if (player.heldItem.type != ItemType.Cup)
        {
            Show(player, "Must be holding a cup");
            return;
        }

        if (!player.heldItem.cupHasBoba)
        {
            Show(player, "Cup needs boba pearls first");
            return;
        }

        if (player.heldItem.cupHasSoda)
        {
            Show(player, "Cannot finalize soda here");
            return;
        }

        player.heldItem.cupBobaDrinkReady = true;
        Show(player, "Boba drink complete!");
        Debug.Log("DrinkTable.Interact completed | cup is ready: " + player.GetHeldItemDebug());
    }
}

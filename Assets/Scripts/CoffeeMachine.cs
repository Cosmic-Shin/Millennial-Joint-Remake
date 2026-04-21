using UnityEngine;

/// <summary>
/// Coffee machine gives coffee instantly with empty hands.
/// No cup required.
/// </summary>
public class CoffeeMachine : BaseStation, IInteractable
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
            Show(player, "Hands must be empty to grab coffee");
            return;
        }

        player.heldItem.Set(ItemType.Cup);
        player.heldItem.cupHasCoffee = true;
        player.RefreshHeldItemDisplay();
        Show(player, "Grabbed coffee!");
    }
}
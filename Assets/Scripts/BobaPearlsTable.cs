using UnityEngine;

public class BobaPearlsTable : BaseStation, IInteractable
{
    public bool CanInteractWith(PlayerControl player) => false;

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        Show(player, "Drink stations are disabled");
    }
}

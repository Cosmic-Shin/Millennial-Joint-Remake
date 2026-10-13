public class PlateCounter : BaseStation, IInteractable
{
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        player.heldItem.MakePlate();
        Show(player, "Picked up plate");
        AudioManager.Instance?.PlayIngredientInTableSFX();
    }
}
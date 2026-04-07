using UnityEngine;

public class IngredientBox : BaseStation, IInteractable
{
    public ItemType ingredientType;

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!player.heldItem.IsEmpty)
        {
            Show(player, "Hands are full");
            return;
        }

        if (ingredientType != ItemType.Bun &&
            ingredientType != ItemType.VeggieRaw &&
            ingredientType != ItemType.PattyRaw)
        {
            Show(player, "Invalid ingredient box setup");
            return;
        }

        player.heldItem.Set(ingredientType);
        Show(player, "Picked up " + player.heldItem.GetDisplayName());
    }
}
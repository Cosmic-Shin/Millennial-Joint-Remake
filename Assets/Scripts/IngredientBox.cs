using UnityEngine;

public class IngredientBox : BaseStation, IInteractable
{
    public ItemType ingredientType;

    // Can only grab from a box when hands are empty
    public bool CanInteractWith(PlayerControl player)
    {
        return player != null && player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        Debug.Log("IngredientBox.Interact called | player holding before: " + player.GetHeldItemDebug());

        if (ingredientType != ItemType.Bun &&
            ingredientType != ItemType.VeggieRaw &&
            ingredientType != ItemType.PattyRaw)
        {
            Show(player, "Invalid ingredient box setup");
            return;
        }

        player.heldItem.Set(ingredientType);
        Debug.Log("IngredientBox.Interact completed | player holding after: " + player.GetHeldItemDebug());
        Show(player, "Picked up " + player.heldItem.GetDisplayName());
    }
}
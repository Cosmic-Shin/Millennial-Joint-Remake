using UnityEngine;
 
public interface IInteractable
{
    /// <summary>
    /// Called when the player presses the interact key.
    /// </summary>
    void Interact(PlayerControl player);
 
    /// <summary>
    /// Returns true if this station can be interacted with given the player's current held item.
    /// Used by PlayerControl to filter out invalid targets before showing the panel.
    /// </summary>
    bool CanInteractWith(PlayerControl player);
}
 
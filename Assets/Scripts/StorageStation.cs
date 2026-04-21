using UnityEngine;

/// <summary>
/// Base class for stations that store items (ChoppingBoard, FryerCounter, CounterTop, StoveCounter).
/// Consolidates common logic: storing items, managing visuals, and showing feedback messages.
/// </summary>
public abstract class StorageStation : BaseStation, IInteractable
{
    [Header("Item Storage")]
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    /// <summary>
    /// Called when the stored item should be updated visually.
    /// Call this whenever storedItem changes.
    /// </summary>
    protected void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }

    public abstract bool CanInteractWith(PlayerControl player);
    public abstract void Interact(PlayerControl player);
}

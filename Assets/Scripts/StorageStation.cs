using UnityEngine;

public abstract class StorageStation : BaseStation, IInteractable
{
    [Header("Item Storage")]
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    protected void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }

    public abstract bool CanInteractWith(PlayerControl player);
    public abstract void Interact(PlayerControl player);
}

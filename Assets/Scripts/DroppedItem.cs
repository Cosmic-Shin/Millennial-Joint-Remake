using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : MonoBehaviour, IInteractable
{
    public KitchenItemData item = new KitchenItemData();
    private KitchenItemVisualizer visualizer;

    public void Initialize(KitchenItemData sourceItem, KitchenItemVisualizer sourceVisualizer)
    {
        if (sourceItem != null)
            item.CopyFrom(sourceItem);

        if (sourceVisualizer != null)
        {
            visualizer = gameObject.AddComponent<KitchenItemVisualizer>();
            visualizer.anchor = transform;
            visualizer.localPosition = Vector3.zero;
            visualizer.localEulerAngles = Vector3.zero;
            visualizer.localScale = Vector3.one;
            // No need to copy prefab references - registry handles them
            visualizer.Refresh(item);
        }
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty && !item.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        if (!player.heldItem.IsEmpty) return;

        player.heldItem.CopyFrom(item);
        player.RefreshHeldItemDisplay();
        Destroy(gameObject);
    }
}

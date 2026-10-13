using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : MonoBehaviour, IInteractable
{
    public KitchenItemData item = new KitchenItemData();
    private KitchenItemVisualizer visualizer;

    private const float fallDestroyY = -15f;

    [HideInInspector] public PlayerControl droppedBy;
    private const float forgetDropperDistance = 2.5f;

    private void Update()
    {
        if (transform.position.y < fallDestroyY)
            Destroy(gameObject);

        if (droppedBy != null && Vector3.Distance(droppedBy.transform.position, transform.position) > forgetDropperDistance)
            droppedBy = null;
    }

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
        AudioManager.Instance?.PlayCounterTopInteractSFX();
        Destroy(gameObject);
    }
}
